"""
Tests: deterministic recommendation metadata in candidate_summary.

Verifies:
1. selection_explanation is present and non-empty.
2. matching_reasons is a non-empty list with sensible content.
3. geometry_fingerprint is preserved (matches catalogue source).
"""
import pytest
from app.design.catalogue.base_plan_library import load_base_plan_catalog
from app.design.generation.generation_service import generate_layout
from app.design.generation.diversity import geometry_fingerprint


def _pick_compatible_plan():
    """Return the first active catalogue plan and safe input params for it."""
    catalog = load_base_plan_catalog()
    assert catalog, "Catalogue must not be empty"
    plan = catalog[0]
    land_size = (plan.minimum_land_perches or 5.0) + 3.0
    return plan, land_size


def test_selection_explanation_present():
    plan, land_size = _pick_compatible_plan()
    result = generate_layout(
        land_size_perches=land_size,
        terrain_type=plan.supported_terrains[0],
        preferences={'bedrooms': plan.bedrooms, 'floors': plan.floors},
        preferred_plan_code=plan.plan_code,
    )
    summary = result.candidate_summary
    assert 'selection_explanation' in summary, "selection_explanation key missing"
    explanation = summary['selection_explanation']
    assert isinstance(explanation, str) and len(explanation) > 10, (
        f"selection_explanation is blank or too short: {explanation!r}"
    )
    assert 'Selected because' in explanation, (
        f"Unexpected explanation format: {explanation!r}"
    )


def test_matching_reasons_present():
    plan, land_size = _pick_compatible_plan()
    result = generate_layout(
        land_size_perches=land_size,
        terrain_type=plan.supported_terrains[0],
        preferences={'bedrooms': plan.bedrooms, 'floors': plan.floors},
        preferred_plan_code=plan.plan_code,
    )
    summary = result.candidate_summary
    assert 'matching_reasons' in summary, "matching_reasons key missing"
    reasons = summary['matching_reasons']
    assert isinstance(reasons, list) and len(reasons) >= 2, (
        f"Expected at least 2 matching reasons, got: {reasons}"
    )
    # Bedroom match must always be present
    bedroom_matches = [r for r in reasons if 'bedroom' in r]
    assert bedroom_matches, f"No bedroom-match reason found in: {reasons}"
    # Floor match must always be present
    floor_matches = [r for r in reasons if 'floor' in r]
    assert floor_matches, f"No floor-match reason found in: {reasons}"


def test_geometry_fingerprint_unchanged():
    """The recommendation metadata must not mutate any geometry."""
    plan, land_size = _pick_compatible_plan()
    original_fingerprint = geometry_fingerprint(plan.design)

    result = generate_layout(
        land_size_perches=land_size,
        terrain_type=plan.supported_terrains[0],
        preferences={'bedrooms': plan.bedrooms, 'floors': plan.floors},
        preferred_plan_code=plan.plan_code,
    )
    assert result.geometry_fingerprint == original_fingerprint, (
        f"Geometry fingerprint mutated after adding recommendation metadata!\n"
        f"  Expected: {original_fingerprint}\n"
        f"  Got:      {result.geometry_fingerprint}"
    )
