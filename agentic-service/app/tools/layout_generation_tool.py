"""Validated base-plan selection with deterministic adaptation and strict quality gates."""

from __future__ import annotations

import json

from typing import Optional, Union

from app.design.architectural_quality import validate_architectural_quality

from app.design.base_plan_library import (

    compact_plan_metadata,

    filter_compatible_base_plans,

    load_base_plan_catalog,

    rank_base_plans,

)

from app.design.candidate_generator import GenerationFailure, select_best

from app.design.diversity import geometry_fingerprint

from app.design.models import Requirements

from app.design.normalized_input import NormalizedDesignInput

from app.design.plan_adapter import PlanAdapter

from app.design.plot_constraints import PlotConstraints

from app.design.revision import apply_supported_revision, requests_another_design

from app.design.scoring import family_affinity

from app.design.topology_registry import eligible_topologies, topology_dict

from app.providers import get_available_design_provider

from app.schemas.ai_plan_decision import AIPlanDecision

from app.schemas.design_result import DesignResult

from app.tools.geometry_validator import validate_geometry

from app.tools.land_utils import MAX_COVERAGE_RATIO, SQFT_PER_PERCH

SYSTEM_PROMPT = (

    "You are the HousePlanner design-selection agent. Choose among validated base plans only. "

    "Do not generate coordinates, room dimensions, openings, or stair locations. Return only the strict schema."

)


def prepare_inputs(

    land_size_perches: float,

    terrain_type: str,

    preferences: dict,

    plot_constraints: Union[dict, Optional[PlotConstraints]] = None,

    design_seed: Optional[int] = None,

) -> tuple[Requirements, PlotConstraints]:

    values = dict(preferences)

    if 'architecturalStyle' in values and 'style' not in values:

        values['style'] = values.pop('architecturalStyle')

    if 'style_preference' in values and 'style' not in values:

        values['style'] = values.pop('style_preference')

    if 'accessibility_preference' in values and 'accessibility' not in values:

        values['accessibility'] = values.pop('accessibility_preference')

    aliases = {

        'architectural_style': 'style',

        'master_ensuite': 'attached_bathroom',

        'separate_dining': 'dining_required',

        'parking_required': 'parking',

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

    values = {key: value for key, value in values.items() if value is not None}

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

    raw.update(

        land_size_perches=land_size_perches,

        terrain_type=terrain_type.lower(),

        parking_reserved=req.parking,

    )

    return req, PlotConstraints.model_validate(raw)



def _candidate_pool(req: Requirements, plot: PlotConstraints):

    compatible = filter_compatible_base_plans(req, plot)

    if not compatible:

        raise GenerationFailure('No compatible validated base plans exist for the supplied requirements.')

    ranked = rank_base_plans(compatible, req, plot)

    diverse = []

    seen_families = set()

    for plan in ranked:

        if plan.topology_family not in seen_families or len(diverse) < 3:

            diverse.append(plan)

            seen_families.add(plan.topology_family)

        if len(diverse) >= min(7, len(ranked)):

            break

    return diverse or ranked[:1]



def _build_ai_prompt(normalized: NormalizedDesignInput, plans, previous_plan_code=None,

                     previous_fingerprint=None, revision_reason: Optional[str] = None) -> str:

    payload = {

        'normalized_input': normalized.model_dump(),

        'candidate_plans': compact_plan_metadata(plans),

        'previous_plan_code': previous_plan_code,

        'previous_fingerprint': previous_fingerprint,

        'revision_reason': revision_reason,

        'generation_mode': 'generate_another' if previous_plan_code else 'generate',

    }

    return json.dumps(payload, separators=(',', ':'), ensure_ascii=True)



def _fallback_decision(plans, req: Requirements, plot: PlotConstraints, previous_plan_code=None,

                       previous_fingerprint=None) -> AIPlanDecision:

    chosen = next((plan for plan in plans if plan.plan_code != previous_plan_code), plans[0])

    alternatives = [plan.plan_code for plan in plans if plan.plan_code != chosen.plan_code][:3]

    public_orientation = plot.road_side

    private_orientation = {'south': 'north', 'north': 'south', 'east': 'west', 'west': 'east'}[plot.road_side]

    service_orientation = {'south': 'west', 'north': 'east', 'east': 'south', 'west': 'north'}[plot.road_side]

    return AIPlanDecision.model_validate({

        'selected_plan_code': chosen.plan_code,

        'alternative_plan_codes': alternatives,

        'design_intent': {

            'public_zone_orientation': public_orientation,

            'private_zone_orientation': private_orientation,

            'service_zone_orientation': service_orientation,

            'privacy_priority': 'high' if getattr(req, 'privacy_priority', False) or getattr(req, 'attached_bathroom', False) else 'balanced',

            'circulation_preference': 'short_central_hall',

        },

        'adaptations': {

            'mirror_horizontal': False,

            'mirror_vertical': False,

            'rotation_degrees': 0,

            'living_scale': 1.0,

            'bedroom_scale': 1.0,

            'entrance_side': plot.effective_entrance_side,

            'preserve_stair_core': True,

            'preserve_wet_core': True,

        },

        'reason_codes': ['plot_fit', 'preference_match', 'low_circulation'],

    })



def _validate_and_finalize(design: DesignResult, req: Requirements, plot: PlotConstraints,

                           base_plan_code: str, provider_name: Optional[str], model_name: Optional[str],

                           ai_decision: AIPlanDecision, tried_codes: list[str], candidate_pool) -> DesignResult:

    quality = validate_architectural_quality(design, req=req, plot=plot)

    if not quality.passed:

        raise GenerationFailure('Architectural quality validation failed.', [{'base_plan_code': base_plan_code, 'failures': quality.failures}])

    geometry = validate_geometry(design.rooms, req.bedrooms, req.floors, plot.land_size_perches, plot=plot, design=design)

    if not geometry.passed:

        raise GenerationFailure('Local geometry validation failed.', [{'base_plan_code': base_plan_code, 'failures': geometry.failures}])

    design.design_score = quality.score

    design.candidate_status = quality.status

    design.geometry_fingerprint = geometry_fingerprint(design)

    if design.candidate_summary is None:
        design.candidate_summary = {}
    design.candidate_summary.update({

        'selected_plan_code': base_plan_code,

        'provider': provider_name,

        'model': model_name,

        'generation_mode': 'ai_adapted_template' if provider_name else 'deterministic_template_selection',

        'quality_metrics': quality.metrics,

        'quality_breakdown': quality.score_breakdown,

        'geometry_validation': geometry.to_dict(),

        'tried_plan_codes': tried_codes,

        'compatible_plan_codes': [plan.plan_code for plan in candidate_pool],

        'catalog_size': len(load_base_plan_catalog()),

    })

    return design



def generate_layout(

    land_size_perches: float,

    terrain_type: str,

    preferences: dict,

    previous_design: Optional[dict] = None,

    revision_reason: Optional[str] = None,

    *,

    plot_constraints: Union[dict, Optional[PlotConstraints]] = None,

    design_seed: Optional[int] = None,

) -> DesignResult:

    try:

        revised_preferences, applied_revision = apply_supported_revision(preferences, revision_reason)

        req, plot = prepare_inputs(land_size_perches, terrain_type, revised_preferences, plot_constraints, design_seed)

        if plot.terrain_type == 'unknown':

            raise GenerationFailure('Terrain is unknown; provide a manual terrain classification.')

        if plot.plot_width_ft and plot.plot_width_ft < 15:

            raise GenerationFailure(f'Plot width ({plot.plot_width_ft} ft) is too narrow for standard construction.')

        if plot.plot_length_ft and plot.plot_length_ft < 15:

            raise GenerationFailure(f'Plot length ({plot.plot_length_ft} ft) is too shallow for standard construction.')

        if plot.buildable_width < 10 or plot.buildable_length < 10:

            raise GenerationFailure('Setbacks leave insufficient buildable area (less than 10ft).')

    except (ValueError, TypeError) as exc:

        raise GenerationFailure(f'Invalid design requirements: {exc}') from exc

    normalized = NormalizedDesignInput.from_inputs(req, plot)

    candidate_pool = _candidate_pool(req, plot)

    previous_plan_code = None

    previous_fingerprint = None

    if previous_design:

        try:

            previous_layout = DesignResult.model_validate(previous_design)

            previous_fingerprint = geometry_fingerprint(previous_layout)

            previous_plan_code = previous_layout.candidate_summary.get('selected_plan_code') if isinstance(previous_layout.candidate_summary, dict) else None

        except Exception:

            previous_fingerprint = None

    if previous_plan_code and requests_another_design(revision_reason):

        candidate_pool = [plan for plan in candidate_pool if plan.plan_code != previous_plan_code] or candidate_pool

    provider = get_available_design_provider()

    provider_name = getattr(provider, 'provider_name', None) if provider else None

    model_name = getattr(provider, 'model_name', None) if provider else None

    if provider:

        print(f'[Design Agent] Calling provider {provider.provider_name} for base-plan selection...')

        user_prompt = _build_ai_prompt(normalized, candidate_pool, previous_plan_code, previous_fingerprint, revision_reason)

        try:

            decision = AIPlanDecision.model_validate(provider.generate_json(SYSTEM_PROMPT, user_prompt, AIPlanDecision))

        except Exception as exc:

            print(f'[Design Agent] AI decision failed ({exc}); using deterministic selection.')

            decision = _fallback_decision(candidate_pool, req, plot, previous_plan_code, previous_fingerprint)

    else:

        decision = _fallback_decision(candidate_pool, req, plot, previous_plan_code, previous_fingerprint)

    if previous_fingerprint and previous_plan_code and decision.selected_plan_code == previous_plan_code:

        decision = decision.model_copy(update={

            'alternative_plan_codes': [code for code in decision.alternative_plan_codes if code != previous_plan_code],

        })

    adapter = PlanAdapter()

    tried_codes: list[str] = []

    failures: list[dict] = []

    candidate_by_code = {plan.plan_code: plan for plan in candidate_pool}

    for plan_code in [decision.selected_plan_code, *decision.alternative_plan_codes]:

        if plan_code in tried_codes:

            continue

        tried_codes.append(plan_code)

        plan = candidate_by_code.get(plan_code)

        if plan is None:

            failures.append({'plan_code': plan_code, 'failures': ['plan_not_compatible']})

            continue

        try:

            design = adapter.adapt(plan, decision, req, plot)

            print(f"DEBUG {plan_code}: {[c.from_room + '-' + c.to_room for c in design.connections]}")

            final_design = _validate_and_finalize(

                design,

                req,

                plot,

                plan.plan_code,

                provider_name,

                model_name,

                decision,

                tried_codes,

                candidate_pool,

            )

            final_design.candidate_summary.update({

                'generation_mode': 'ai_adapted_template' if provider_name else 'deterministic_template_selection',

                'base_plan_name': plan.name,

                'base_plan_code': plan.plan_code,
                
                'template_id': final_design.template_id,

                'alternative_plan_codes': decision.alternative_plan_codes,

                'reason_codes': decision.reason_codes,

                'normalized_input': normalized.model_dump(),

                'compatible_plan_count': len(candidate_pool),

            })

            if previous_fingerprint:

                final_design.candidate_summary['previous_fingerprint'] = previous_fingerprint

            if previous_plan_code:

                final_design.candidate_summary['previous_plan_code'] = previous_plan_code

            if revision_reason:

                final_design.candidate_summary['revision_feedback'] = revision_reason

                final_design.candidate_summary['bounded_revision_preferences'] = applied_revision

            return final_design

        except GenerationFailure as exc:

            failures.extend(getattr(exc, 'failures', []) or [{'plan_code': plan_code, 'failures': [str(exc)]}])

        except Exception as exc:

            failures.append({'plan_code': plan_code, 'failures': [str(exc)]})
    if failures and provider:
        print("ADAPTATION FAILURES FROM AI:", json.dumps(failures, indent=2))
        print("Falling back to deterministic selection.")
        decision = _fallback_decision(candidate_pool, req, plot, previous_plan_code, previous_fingerprint)
        tried_codes.clear()
        for plan_code in [decision.selected_plan_code, *decision.alternative_plan_codes]:
            if plan_code in tried_codes:
                continue
            tried_codes.append(plan_code)
            plan = candidate_by_code.get(plan_code)
            if not plan:
                continue
            try:
                design = adapter.adapt(plan, decision, req, plot)
                return _validate_and_finalize(design, req, plot, plan.plan_code, None, None, decision, tried_codes, candidate_pool)
            except GenerationFailure as exc:
                failures.extend(getattr(exc, 'failures', []) or [{'plan_code': plan_code, 'failures': [str(exc)]}])
            except Exception as exc:
                failures.append({'plan_code': plan_code, 'failures': [str(exc)]})

    if failures:
        raise GenerationFailure('No validated base plan could be adapted into a high-quality design.', failures)
    raise GenerationFailure('Candidate pool exhausted without finding a valid plan.')





def select_template(bedrooms: int, floors: int, terrain_type: str, land_size_perches: float) -> dict:

    """Legacy helper name retained; returns a validated base-plan summary, never coordinates."""

    import math

    side = round(math.sqrt(land_size_perches * 272.25), 1)

    req, plot = prepare_inputs(

        land_size_perches,

        terrain_type,

        {'bedrooms': bedrooms, 'floors': floors},

        plot_constraints={'plot_width_ft': side, 'plot_length_ft': side},

    )

    choices = eligible_topologies(req, plot)

    if not choices:

        raise GenerationFailure('No eligible topology for the plot and requirements.')

    topology = max(choices, key=lambda topo: family_affinity(topo.name, req, plot))

    return {

        'name': topology.name,

        'plan_code': topology.name,

        'min_width': topology.min_width,

        'min_length': topology.min_length,

        'supported_floors': list(topology.supported_floors),

        'bedroom_range': list(topology.bedroom_range),

        'zoning': topology.zoning,

        'adjacency': list(topology.adjacency),

    }





def _mock_layout(
    bedrooms: int,
    floors: int,
    terrain_type: str,
    foundation_type: str,
    max_area: float,
    template_id: Optional[str] = None,
    template: Optional[dict] = None,
) -> DesignResult:
    """Compatibility wrapper: the offline path uses the same validated base-plan engine."""
    import math
    perches = max_area / (SQFT_PER_PERCH * MAX_COVERAGE_RATIO)
    side = round(math.sqrt(perches * 272.25), 1)
    
    result = generate_layout(
        land_size_perches=perches,
        terrain_type=terrain_type,
        preferences={'bedrooms': bedrooms, 'floors': floors, 'design_seed': 0},
        plot_constraints={'plot_width_ft': side, 'plot_length_ft': side}
    )
    
    if template_id:
        result.template_id = template_id
    result.foundation_type = foundation_type
    return result

