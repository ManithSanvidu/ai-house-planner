import pytest
from app.design.generation.generation_service import generate_layout

def test_unsupported_feature_combinations_succeed():
    """
    User can submit unsupported feature combinations.
    Python returns closest compatible catalogue plan or meaningful failure.
    """
    # 3 bed, 2 bath, 1 floor is supported by basic plans.
    # But requesting ALL features (balcony, home_office, separate_dining, utility_room, veranda)
    # is a combination not supported by the strict JSON catalogue for this size.
    # The relaxed validation should allow this to pass and return the best match.
    preferences = {
        'bedrooms': 3,
        'bathrooms': 2,
        'floors': 1,
        'style': 'Tropical',
        'balcony': True,
        'home_office': True,
        'separate_dining': True,
        'utility_room': True,
        'veranda': True
    }
    
    design = generate_layout(
        land_size_perches=15.0,
        terrain_type='flat',
        preferences=preferences,
        plot_constraints={'plot_width_ft': 60, 'plot_length_ft': 80}
    )
    
    assert design is not None
    assert design.candidate_summary is not None
    
    # Verify we got a plan
    assert design.candidate_summary.get('base_plan_code') is not None
    
    # We shouldn't fail due to feature mismatch
    assert len(design.candidate_summary.get('compatible_plan_codes', [])) > 0

def test_existing_exact_matches_work():
    """
    Existing exact matches still work perfectly.
    """
    # A known exact match profile (e.g. standard 3B2B1F compact)
    preferences = {
        'bedrooms': 3,
        'bathrooms': 2,
        'floors': 1,
        'style': 'Modern',
        'compact_priority': True
    }
    
    design = generate_layout(
        land_size_perches=15.0,
        terrain_type='flat',
        preferences=preferences,
        plot_constraints={'plot_width_ft': 60, 'plot_length_ft': 80}
    )
    
    assert design is not None
    assert 'COMPACT_RECTANGLE' in design.template_family or 'CENTRAL_CORE' in design.template_family
