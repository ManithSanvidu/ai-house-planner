from typing import Optional, Union
"""Generative AI layout planner with procedural fallback."""
import json
import uuid
from app.config import OPENAI_API_KEY
from app.design.candidate_generator import GenerationFailure, select_best
from app.design.diversity import geometry_fingerprint, stable_seed
from app.design.geometry_engine import generate_geometry, TERRAIN_FOUNDATION_MAP
from app.design.models import Requirements
from app.design.scoring import score_layout
from app.design.revision import apply_supported_revision, requests_another_design
from app.design.quality_metrics import (
    CIRCULATION_VERY_POOR_RATIO, HALLWAY_EXTREME_LENGTH_FT,
    NARROW_PLOT_THRESHOLD_FT, calculate_quality_metrics, quality_feedback,
)
from app.design.plot_constraints import PlotConstraints
from app.design.spatial_program import build_program
from app.design.topology_registry import eligible_topologies, topology_dict
from app.schemas.design_result import DesignResult
from app.schemas.design_strategy import DesignStrategy
from app.tools.geometry_validator import validate_geometry
from app.tools.land_utils import SQFT_PER_PERCH, MAX_COVERAGE_RATIO
from app.providers import get_available_design_provider

AI_CANDIDATE_COUNT = 3

SYSTEM_PROMPT = """You are the HousePlanner Design Strategy Agent.
Choose a conceptual topology and zoning strategy for the provided plot and requirements.
Do not generate room coordinates, dimensions, doors, windows, or explanations.
Return only data matching the required schema."""

# In-memory local cache for strategies
_strategy_cache = {}

def prepare_inputs(land_size_perches: float, terrain_type: str, preferences: dict,
                   plot_constraints: Union[dict, Optional[PlotConstraints]] = None,
                   design_seed: Optional[int] = None) -> tuple[Requirements, PlotConstraints]:
    values = dict(preferences)
    if 'architecturalStyle' in values and 'style' not in values:
        values['style'] = values.pop('architecturalStyle')
    if 'style_preference' in values and 'style' not in values:
        values['style'] = values.pop('style_preference')
    if 'accessibility_preference' in values and 'accessibility' not in values:
        values['accessibility'] = values.pop('accessibility_preference')
    aliases = {
        'architectural_style': 'style', 'master_ensuite': 'attached_bathroom',
        'separate_dining': 'dining_required', 'parking_required': 'parking',
        'utility': 'utility_room',
    }
    for source_key, target_key in aliases.items():
        if source_key in values and target_key not in values:
            values[target_key] = values.pop(source_key)
    if values.get('space_priority') == 'outdoor_garden':
        values['garden_priority'] = True
    if values.get('space_priority') == 'compact_cost_efficient':
        values['compact_priority'] = True
    if values.get('attached_bathroom'):
        values['master_bedroom'] = True
    if design_seed is not None:
        values['design_seed'] = design_seed
    values = {k: v for k, v in values.items() if v is not None}
    req = Requirements.model_validate(values)
    source = plot_constraints if plot_constraints is not None else preferences.get('plot_constraints', {})
    if isinstance(source, PlotConstraints):
        source = source.model_dump(include=set(PlotConstraints.model_fields))
    raw = dict(source)
    if raw.get('entrance_side') == 'road_side':
        raw['entrance_side'] = raw.get('road_side', preferences.get('road_side', 'south'))
    for key in PlotConstraints.model_fields:
        if key in preferences and key not in raw:
            raw[key] = preferences[key]
    raw.update(land_size_perches=land_size_perches, terrain_type=terrain_type.lower(), parking_reserved=req.parking)
    return req, PlotConstraints.model_validate(raw)


def generate_layout(land_size_perches: float, terrain_type: str, preferences: dict,
                    previous_design: Optional[dict] = None, revision_reason: Optional[str] = None,
                    *, plot_constraints: Union[dict, Optional[PlotConstraints]] = None,
                    design_seed: Optional[int] = None, budget_lkr: Optional[float] = None) -> DesignResult:
    try:
        revised_preferences, applied_revision = apply_supported_revision(preferences, revision_reason)
        req, plot = prepare_inputs(land_size_perches, terrain_type, revised_preferences, plot_constraints, design_seed)
    except (ValueError, TypeError) as exc:
        raise GenerationFailure(f'Invalid design requirements: {exc}') from exc
        
    mode = 'procedural_strategy'
    provider_name = None
    model_name = None
    strategy = None

    provider = get_available_design_provider()
    if provider:
        print(f"[Design Agent] Calling provider {provider.provider_name} for strategy...")
        mode = 'llm_strategy'
        provider_name = provider.provider_name
        model_name = provider.model_name
        
        eligible_families = [topology_dict(t) for t in eligible_topologies(req, plot)]
        
        # Build compact DTO to aggressively save input tokens
        from app.schemas.design_strategy import DesignStrategyRequest, LandSummary
        
        # Strip all extra history, prior coordinates, and validation junk from requirements
        clean_req = req.model_dump(exclude_unset=True)
        # Only keep scalar fields that affect strategy. Exclude scaling factors or seeds to maximize cache hits.
        exclude_from_strategy = {"design_seed", "living_area_scale", "kitchen_area_scale"}
        strategy_reqs = {k: v for k, v in clean_req.items() if isinstance(v, (int, float, str, bool)) and k not in exclude_from_strategy}
        
        req_dto = DesignStrategyRequest(
            land=LandSummary(
                perches=plot.land_size_perches,
                area_sqft=plot.land_size_perches * SQFT_PER_PERCH,
                width_ft=plot.plot_width_ft,
                length_ft=plot.plot_length_ft,
                buildable_width_ft=plot.buildable_width,
                buildable_length_ft=plot.buildable_length,
                terrain=plot.terrain_type,
                road_side=plot.effective_entrance_side
            ),
            requirements=strategy_reqs,
            eligible_families=[f["name"] for f in eligible_families]
        )
        user_prompt = req_dto.model_dump_json()
        
        import hashlib
        prompt_hash = hashlib.md5(user_prompt.encode('utf-8')).hexdigest()
        
        if prompt_hash in _strategy_cache:
            print(f"[Design Agent] Found cached strategy for prompt hash {prompt_hash}. Bypassing LLM.")
            strategy = _strategy_cache[prompt_hash]
            mode = 'llm_strategy_cached'
        else:
            try:
                data = provider.generate_json(SYSTEM_PROMPT, user_prompt, DesignStrategy)
                allowed = {f['name'] for f in eligible_families}
                if data.get('topology_family') not in allowed:
                    data['topology_family'] = list(allowed)[0] if allowed else 'linear'
                strategy = DesignStrategy.model_validate(data)
                _strategy_cache[prompt_hash] = strategy
                print(f"[Design Agent] Strategy generated: {strategy.topology_family}")
            except Exception as exc:
                print(f"[Design Agent] LLM generation failed ({exc}). Falling back to procedural strategy.")
                mode = 'procedural_fallback'

    if not strategy:
        # Deterministic strategy fallback
        choices = eligible_topologies(req, plot)
        from app.design.scoring import family_affinity
        if choices:
            chosen_topology = max(choices, key=lambda t: family_affinity(t.name, req, plot))
            chosen_family = chosen_topology.name
        else:
            chosen_family = 'COMPACT_RECTANGLE'
            
        strategy = DesignStrategy(
            topology_family=chosen_family,
            public_zone="Standard open plan living/dining",
            private_zone="Clustered bedrooms",
            service_zone="Grouped wet zone",
            circulation_strategy="direct",
            entrance_side=plot.effective_entrance_side or "south",
            bedroom_strategy="Standard",
            kitchen_relationship="Open to dining",
            wet_zone_strategy="Grouped",
            style_intent=req.style
        )

    # Now use the strategy to procedurally generate 3 geometry candidates
    valid_candidates = []
    failures = []
    
    # We do 3 variants (0, 1, 2)
    for candidate_index in range(AI_CANDIDATE_COUNT):
        try:
            candidate = generate_geometry(build_program(req), req, plot, strategy.topology_family, req.design_seed or 0, candidate_index)
            candidate.total_built_up_area_sqft = round(sum(r.width * r.length for r in candidate.rooms), 2)
            candidate.ground_footprint_sqft = round(sum(r.width * r.length for r in candidate.rooms if r.floor == 1), 2)
            
            # Final validation check
            check = validate_geometry(candidate.rooms, req.bedrooms, req.floors, land_size_perches, plot=plot, design=candidate)
            if check.passed:
                candidate.design_score, breakdown = score_layout(candidate, req, plot)
                # Assign geometry fingerprint from the existing diversity method or a simple hash
                candidate.geometry_fingerprint = f"{strategy.topology_family}_{candidate_index}"
                candidate.candidate_summary = {
                    'candidate_index': candidate_index,
                    'score_breakdown': breakdown,
                    'provider': provider_name,
                    'model': model_name,
                    'generation_mode': mode
                }
                valid_candidates.append(candidate)
            else:
                foot_w = max(r.x + r.width for r in candidate.rooms)
                foot_l = max(r.y + r.length for r in candidate.rooms)
                print(f"Candidate {candidate_index} failed: width={foot_w} vs buildable={plot.buildable_width}, length={foot_l} vs buildable={plot.buildable_length}. Topology: {strategy.topology_family}")
                failures.append({'candidate': candidate_index, 'failures': check.failures})
        except Exception as exc:
            failures.append({'candidate': candidate_index, 'failures': [str(exc)]})
            
    if not valid_candidates:
        if mode != 'procedural_fallback':
            print(f"[Design Agent] LLM strategy {strategy.topology_family} failed to generate valid geometry. Falling back to procedural.")
            mode = 'procedural_fallback'
            choices = eligible_topologies(req, plot)
            if choices:
                from app.design.scoring import family_affinity
                chosen_topology = max(choices, key=lambda t: family_affinity(t.name, req, plot))
                strategy.topology_family = chosen_topology.name
            else:
                strategy.topology_family = 'COMPACT_RECTANGLE'
            
            # Retry generation with the fallback topology
            failures.clear()
            for candidate_index in range(AI_CANDIDATE_COUNT):
                try:
                    candidate = generate_geometry(build_program(req), req, plot, strategy.topology_family, req.design_seed or 0, candidate_index)
                    candidate.total_built_up_area_sqft = round(sum(r.width * r.length for r in candidate.rooms), 2)
                    candidate.ground_footprint_sqft = round(sum(r.width * r.length for r in candidate.rooms if r.floor == 1), 2)
        
                    check = validate_geometry(candidate.rooms, req.bedrooms, req.floors, land_size_perches, plot=plot, design=candidate)
                    if check.passed:
                        candidate.design_score, breakdown = score_layout(candidate, req, plot)
                        candidate.geometry_fingerprint = f"{strategy.topology_family}_{candidate_index}"
                        candidate.candidate_summary = {
                            'candidate_index': candidate_index,
                            'score_breakdown': breakdown,
                            'provider': provider_name,
                            'model': model_name,
                            'generation_mode': mode
                        }
                        valid_candidates.append(candidate)
                    else:
                        failures.append({'candidate': candidate_index, 'failures': check.failures})
                except Exception as exc:
                    failures.append({'candidate': candidate_index, 'failures': [str(exc)]})

    if not valid_candidates:
        raise GenerationFailure(f"Deterministic geometry engine failed to produce valid candidates. Failures: {failures}")
        
    # Pick the best valid candidate
    result = max(valid_candidates, key=lambda c: c.design_score)
    result.candidate_summary.update({
        'generation_mode': mode,
        'generated_count': AI_CANDIDATE_COUNT,
        'valid_count': len(valid_candidates),
        'rejected_attempt_count': len(failures),
        'provider': provider_name,
        'model': model_name
    })

    known_extras = set(PlotConstraints.model_fields) | {'plot_constraints', 'photo_url'}
    unhandled = sorted(set(req.model_extra or {}) - known_extras)
    result.candidate_summary.update(generation_mode=mode, unhandled_preferences=unhandled)
    final_metrics = calculate_quality_metrics(result)
    result.candidate_summary['quality_metrics'] = final_metrics
    result.candidate_summary['quality_feedback'] = quality_feedback(final_metrics)
    if (final_metrics['circulation_ratio'] > 0.12 and
            min(plot.buildable_width, plot.buildable_length) < NARROW_PLOT_THRESHOLD_FT):
        result.candidate_summary['circulation_exception'] = (
            'Higher circulation ratio retained because the buildable plot is narrow; '
            'the score still includes the circulation penalty.'
        )
    if unhandled:
        result.candidate_summary.setdefault('notes', []).append('Unrecognized preferences were not applied: '+', '.join(unhandled))
    if revision_reason:
        result.candidate_summary['revision_feedback'] = revision_reason
        result.candidate_summary['bounded_revision_preferences'] = applied_revision
        
    return result





def select_template(bedrooms: int, floors: int, terrain_type: str, land_size_perches: float) -> dict:
    """Legacy helper name retained; returns topology rules, never finished coordinates."""
    import math
    side = round(math.sqrt(land_size_perches * 272.25), 1)
    req, plot = prepare_inputs(land_size_perches, terrain_type, {'bedrooms': bedrooms, 'floors': floors},
                               plot_constraints={'plot_width_ft': side, 'plot_length_ft': side})
    choices = eligible_topologies(req, plot)
    if not choices:
        raise GenerationFailure('No eligible topology for the plot and requirements.')
    from app.design.scoring import family_affinity
    topology = max(choices, key=lambda t: family_affinity(t.name, req, plot))
    return {'template_id': f'{bedrooms}BR_{floors}F_{terrain_type.upper()}', **topology_dict(topology)}


def _mock_layout(bedrooms: int, floors: int, terrain_type: str, foundation_type: str,
                 max_area: float, template_id: Optional[str] = None, template: Optional[dict] = None) -> DesignResult:
    """Compatibility wrapper: the offline path uses the same validated candidate engine."""
    import math
    perches = max_area/(SQFT_PER_PERCH*MAX_COVERAGE_RATIO)
    side = round(math.sqrt(perches * 272.25), 1)
    req, plot = prepare_inputs(perches, terrain_type,
                               {'bedrooms': bedrooms, 'floors': floors, 'design_seed': 0},
                               plot_constraints={'plot_width_ft': side, 'plot_length_ft': side})
    result = select_best(req, plot)
    if template_id:
        result.template_id = template_id
    return result
