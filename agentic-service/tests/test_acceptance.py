import pytest
from app.design.plot_constraints import PlotConstraints
from app.design.models import Requirements
from app.tools.layout_generation_tool import prepare_inputs, generate_layout
from app.tools.geometry_validator import validate_geometry

def test_10_seed_acceptance():
    """
    User Acceptance Test:
    terrain = flat
    land = 15 perches
    bedrooms = 3
    bathrooms = 2
    floors = 1
    style = modern
    road = south
    
    Test 10 different seeds to ensure 100% valid geometry (0 overlaps, connected).
    """
    valid_count = 0
    unique_fingerprints = set()

    for seed in range(1, 11):
        preferences = {
            'bedrooms': 3,
            'bathrooms': 2,
            'floors': 1,
            'style': 'modern',
            'road_side': 'south',
            'design_seed': seed
        }
        
        # In testing, we might not have API keys, so it falls back to procedural strategy
        result = generate_layout(
            land_size_perches=15.0,
            terrain_type='flat',
            preferences=preferences,
            design_seed=seed
        )
        
        req, plot = prepare_inputs(15.0, 'flat', preferences)
        check = validate_geometry(result.rooms, req.bedrooms, req.floors, 15.0, plot=plot, design=result)
        
        assert check.passed, f"Seed {seed} failed validation: {check.failures}"
        
        valid_count += 1
        unique_fingerprints.add(result.geometry_fingerprint)
        
    assert valid_count == 10, "Not all seeds produced valid designs."

if __name__ == "__main__":
    test_10_seed_acceptance()
    print("Acceptance test passed!")
