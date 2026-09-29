from __future__ import annotations

"""Validated base-plan selection with deterministic adaptation and strict quality gates."""


import json
import logging
from collections import Counter

from app.design.quality.architectural_quality import validate_architectural_quality
from app.design.catalogue.base_plan_library import (
    compact_plan_metadata,
    compatibility_rejection_reasons,
    deduplicate_base_plans,
    filter_compatible_base_plans,
    load_base_plan_catalog,
    rank_base_plans,
)
from app.design.exceptions import GenerationFailure
from app.design.generation.diversity import geometry_fingerprint
from app.design.program.models import Requirements
from app.design.program.normalized_input import NormalizedDesignInput

from app.design.catalogue.plan_suitability import suitability_breakdown
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.generation.revision import (
    apply_supported_revision,
    preserve_revision_preferences,
    requests_another_design,
)
from app.design.quality.scoring import family_affinity
from app.design.geometry.topology_registry import eligible_topologies
from app.schemas.design_result import DesignResult
from app.validation.geometry_validator import validate_geometry
from app.land.land_math import MAX_COVERAGE_RATIO, SQFT_PER_PERCH


logger = logging.getLogger(__name__)


def prepare_inputs(

    land_size_perches: float,

    terrain_type: str,

    preferences: dict,

    plot_constraints: dict | PlotConstraints | None = None,

    design_seed: int | None = None,

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



NO_DISTINCT_LAYOUT = 'No distinct compatible layout is currently available for these requirements.'


# ---------------------------------------------------------------------------
# Deterministic recommendation metadata
# ---------------------------------------------------------------------------

_REASON_LABELS: dict[str, str] = {
    'missing_home_office': 'home office not available',
    'missing_accessibility': 'accessible layout not available',
    'missing_master_ensuite': 'master ensuite not available',
    'missing_open_plan': 'open-plan living not available',
    'missing_separate_dining': 'separate dining room not available',
    'missing_balcony': 'balcony not available',
    'missing_veranda': 'veranda not available',
    'missing_utility_room': 'utility room not available',
    'missing_parking': 'parking not available',
    'floor_count': 'floor count mismatch',
    'bedroom_count': 'bedroom count mismatch',
    'bathroom_count': 'insufficient bathrooms',
    'minimum_land': 'plot too small',
    'terrain': 'terrain incompatible',
    'plot_shape': 'plot shape not supported',
    'footprint_width': 'footprint too wide for plot',
    'footprint_length': 'footprint too long for plot',
    'inactive': 'plan inactive',
}


def _build_recommendation_metadata(
    plan,
    candidate_pool: list,
    req: Requirements,
    plot: PlotConstraints,
) -> dict:
    """Return deterministic explanation, matching reasons, and rejected alternatives.
    No LLM is called. All text is derived from existing compatibility/suitability data.
    """
    # ---- matching reasons ----
    reasons: list[str] = []
    reasons.append(f'{plan.bedrooms} bedroom{"s" if plan.bedrooms != 1 else ""} matched')
    reasons.append(f'{plan.bathrooms} bathroom{"s" if plan.bathrooms != 1 else ""} matched')
    reasons.append(
        'single floor requirement matched' if plan.floors == 1
        else f'{plan.floors}-floor requirement matched'
    )
    if req.style and req.style.casefold() in {s.casefold() for s in plan.supported_styles}:
        reasons.append(f'{req.style} style matched')
    feature_map = [
        ('home_office',      req.home_office,                      'home office available'),
        ('master_ensuite',   req.master_bedroom or req.attached_bathroom, 'master ensuite available'),
        ('open_plan',        req.open_plan,                        'open-plan living available'),
        ('separate_dining',  req.dining_required,                  'separate dining room available'),
        ('balcony',          req.balcony,                          'balcony available'),
        ('veranda',          req.veranda,                          'veranda available'),
        ('utility_room',     req.utility_room,                     'utility room available'),
        ('parking',          req.parking,                          'parking available'),
        ('accessibility',    req.accessibility,                    'accessible layout available'),
    ]
    for cap_key, requested, label in feature_map:
        if requested and plan.capabilities.get(cap_key, False):
            reasons.append(label)

    breakdown = suitability_breakdown(plan, req, plot)
    if breakdown['plot_fit']['shape_supported']:
        reasons.append('plot shape supported')
    if breakdown['plot_fit']['entrance_match']:
        reasons.append('entrance orientation matched')

    # ---- selection explanation ----
    core = [f'{plan.bedrooms} bedroom', f'{plan.bathrooms} bathroom',
            f'{"single" if plan.floors == 1 else plan.floors}-floor']
    feature_labels = [r for _, req_val, label in feature_map if req_val for r in ([label] if plan.capabilities.get(_, False) else [])]
    matched_parts = ', '.join(core + (feature_labels[:3] if feature_labels else []))
    explanation = (
        f'Selected because it matches {matched_parts} '
        f'and scored highest ({breakdown["score"]:.0f}/100) among {len(candidate_pool)} '
        f'compatible plan{"s" if len(candidate_pool) != 1 else ""}.'
    )

    # ---- rejected alternatives (runner-up plans) ----
    rejected_alternatives: list[dict] = []
    for alt in candidate_pool[1:4]:          # up to 3 runner-ups
        alt_reasons = compatibility_rejection_reasons(alt, req, plot)
        # Runner-ups passed hard compatibility, so rejection here means lower score
        alt_breakdown = suitability_breakdown(alt, req, plot)
        score_gap = round(breakdown['score'] - alt_breakdown['score'], 1)
        missing_caps = [_REASON_LABELS.get(r, r.replace('_', ' ')) for r in alt_reasons]
        reason_text = (
            '; '.join(missing_caps[:2]) if missing_caps
            else f'lower suitability score (gap: {score_gap} points)'
        )
        rejected_alternatives.append({
            'plan_code': alt.plan_code,
            'plan_name': alt.name,
            'score': alt_breakdown['score'],
            'reason': reason_text,
        })

    return {
        'selection_explanation': explanation,
        'matching_reasons': reasons,
        'rejected_alternatives': rejected_alternatives,
    }



def _candidate_pool(req: Requirements, plot: PlotConstraints, previous_fingerprint=None,
                    preferred_plan_code: str | None = None,
                    excluded_plan_code: str | None = None):

    compatible = filter_compatible_base_plans(req, plot)

    if excluded_plan_code:
        # We explicitly exclude this plan code if alternatives exist.
        # But if it's the ONLY plan, we might have to use it (or fail). The user requested to fail if no alternatives.
        compatible = [plan for plan in compatible if plan.plan_code != excluded_plan_code]
        if not compatible:
            raise GenerationFailure('No sufficiently different compatible design is currently available.')

    if preferred_plan_code:
        compatible = [plan for plan in compatible if plan.plan_code == preferred_plan_code]
        if not compatible:
            raise GenerationFailure('The selected base plan is not compatible with these requirements.')

    rejected = Counter(reason for plan in load_base_plan_catalog()
                       for reason in compatibility_rejection_reasons(plan, req, plot))
    logger.info('[Candidate Filter] compatible_plan_codes=%s rejected_reasons=%s',
                [plan.plan_code for plan in compatible], dict(sorted(rejected.items())))

    if not compatible:

        raise GenerationFailure('No compatible validated base plans exist for the supplied requirements.')

    ranked = deduplicate_base_plans(rank_base_plans(compatible, req, plot), previous_fingerprint)
    if not ranked:
        raise GenerationFailure(NO_DISTINCT_LAYOUT)

    logger.info('[Suitability Ranking] candidates=%s', [
        {'plan_code': plan.plan_code, 'topology': plan.topology_family,
         'suitability_score': suitability_breakdown(plan, req, plot)['score']}
        for plan in ranked
    ])

    diverse = []

    seen_families = set()

    remaining = []
    for plan in ranked:
        if plan.topology_family not in seen_families:
            diverse.append(plan)
            seen_families.add(plan.topology_family)
        else:
            remaining.append(plan)

    # Retain the complete unique pool for fallback beyond the AI shortlist.
    return diverse + remaining







def generate_layout(
    land_size_perches: float,
    terrain_type: str,
    preferences: dict,
    previous_design: dict | None = None,
    revision_reason: str | None = None,
    *,
    plot_constraints: dict | PlotConstraints | None = None,
    design_seed: int | None = None,
    preferred_plan_code: str | None = None,
    excluded_plan_code: str | None = None,
    excluded_fingerprint_explicit: str | None = None,
) -> DesignResult:
    try:
        if previous_design is not None:
            preferences = preserve_revision_preferences(preferences, previous_design)
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
    logger.info('[Design Input] normalized=%s', normalized.model_dump())

    previous_plan_code = None
    previous_fingerprint = None

    if requests_another_design(revision_reason) and previous_design is None:
        raise GenerationFailure('Cannot compare geometry: the previous design is required.')

    if previous_design is not None:
        try:
            previous_layout = DesignResult.model_validate(previous_design)
            previous_fingerprint = geometry_fingerprint(previous_layout)
            previous_plan_code = previous_layout.candidate_summary.get('selected_plan_code') if isinstance(previous_layout.candidate_summary, dict) else None
        except Exception as exc:
            if requests_another_design(revision_reason):
                raise GenerationFailure('Cannot compare geometry: the previous design is invalid.') from exc
            previous_fingerprint = None

    excluded_fingerprint = excluded_fingerprint_explicit or (previous_fingerprint if requests_another_design(revision_reason) else None)
    
    # Get the compatible candidate pool directly from base_plan_library logic
    candidate_pool = _candidate_pool(req, plot, excluded_fingerprint, preferred_plan_code, excluded_plan_code)
    
    if not candidate_pool:
        raise GenerationFailure('Candidate pool exhausted without finding a valid plan.')
        
    # Deterministically select the top ranked plan
    plan = candidate_pool[0]
    
    # Deep copy the design so we don't modify the cached catalog instance
    final_design = plan.design.model_copy(deep=True)
    
    # Validate the pure catalog plan to append score and metrics without strict preference rejection
    quality = validate_architectural_quality(final_design, req=None, plot=plot)
    if not quality.passed:
        raise GenerationFailure('Architectural quality validation failed on catalog plan.', [{'base_plan_code': plan.plan_code, 'failures': quality.failures}])
        
    geometry = validate_geometry(final_design.rooms, req.bedrooms, getattr(final_design, 'floor_count', req.floors), plot.land_size_perches, plot=plot, design=final_design)
    if not geometry.passed:
        raise GenerationFailure('Geometry validation failed on catalog plan.', [{'base_plan_code': plan.plan_code, 'failures': geometry.failures}])
        
    final_design.design_score = quality.score
    final_design.candidate_status = quality.status
    final_design.geometry_fingerprint = geometry_fingerprint(final_design)
    
    if final_design.candidate_summary is None:
        final_design.candidate_summary = {}
        
    recommendation = _build_recommendation_metadata(plan, candidate_pool, req, plot)

    final_design.candidate_summary.update({
        'generation_mode': 'deterministic_template_selection',
        'ai_ran': False,
        'base_plan_name': plan.name,
        'base_plan_code': plan.plan_code,
        'selected_plan_code': plan.plan_code,
        'template_id': final_design.template_id,
        'normalized_input': normalized.model_dump(),
        'compatible_plan_count': len(candidate_pool),
        'tried_plan_codes': [plan.plan_code],
        'compatible_plan_codes': [p.plan_code for p in candidate_pool],
        'catalog_size': len(load_base_plan_catalog()),
        'quality_metrics': quality.metrics,
        'quality_breakdown': quality.score_breakdown,
        'geometry_validation': geometry.to_dict(),
        # Deterministic recommendation metadata
        'selection_explanation': recommendation['selection_explanation'],
        'matching_reasons': recommendation['matching_reasons'],
        'rejected_alternatives': recommendation['rejected_alternatives'],
    })
    
    if previous_fingerprint:
        final_design.candidate_summary['previous_fingerprint'] = previous_fingerprint
    if previous_plan_code:
        final_design.candidate_summary['previous_plan_code'] = previous_plan_code
    if revision_reason:
        final_design.candidate_summary['revision_feedback'] = revision_reason
        final_design.candidate_summary['bounded_revision_preferences'] = applied_revision
        
    logger.info('[Final Design] selected_base_plan=%s topology=%s fingerprint=%s generation_mode=%s',
                plan.plan_code, final_design.template_family,
                final_design.geometry_fingerprint,
                final_design.candidate_summary.get('generation_mode'))
                
    return final_design


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
    template_id: str | None = None,
    template: dict | None = None,
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
