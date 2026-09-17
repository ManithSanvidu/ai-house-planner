import pytest
from app.tools.layout_generation_tool import generate_layout, prepare_inputs
from app.tools.geometry_validator import validate_geometry

def test_land_scaling_matrix():
    """
    Test matrix for land size scaling logic.
    Ensures that as land size increases, the footprint and topologies scale properly.
    """
    perches_to_test = [8, 12, 20, 30, 40, 50]
    
    preferences = {
        'bedrooms': 3,
        'bathrooms': 2,
        'floors': 1,
        'style': 'modern',
        'road_side': 'south',
        'design_seed': 42  # Use fixed seed for deterministic comparison
    }

    results = []
    
    print("\n\n" + "="*80)
    print("LAND SCALING TEST MATRIX REPORT")
    print("="*80)
    print(f"{'Perch':<10} | {'Plot Class':<20} | {'Topology':<20} | {'Buildable (ft)':<15} | {'Built-up Area':<15} | {'Living Sqft':<15} | {'Avg Bed Sqft':<15}")
    print("-" * 125)

    for p in perches_to_test:
        # We pass plot_constraints=None to force it to estimate dimensions internally
        result = generate_layout(
            land_size_perches=p,
            terrain_type='flat',
            preferences=preferences,
            design_seed=42
        )
        
        req, plot = prepare_inputs(p, 'flat', preferences, design_seed=42)
        
        # Validation Check
        check = validate_geometry(result.rooms, req.bedrooms, req.floors, p, plot=plot, design=result)
        assert check.passed, f"Geometry validation failed for {p} perches: {check.failures}"
        
        # Calculate specific room metrics
        living_area = next((r.width * r.length for r in result.rooms if r.room_type == 'living_room'), 0)
        bedrooms = [r.width * r.length for r in result.rooms if 'bedroom' in r.room_type]
        avg_bed = sum(bedrooms) / max(len(bedrooms), 1) if bedrooms else 0
        
        print(f"{p:<10} | {plot.plot_class:<20} | {result.template_family:<20} | {round(plot.buildable_width)}x{round(plot.buildable_length):<11} | {result.total_built_up_area_sqft:<15} | {round(living_area):<15} | {round(avg_bed):<15}")
        
        results.append({
            'perch': p,
            'built_up_area': result.total_built_up_area_sqft,
            'living_area': living_area,
            'avg_bed': avg_bed,
            'topology': result.template_family
        })
        
    print("="*80 + "\n")

    # Assertions to ensure material scaling occurs
    assert results[-1]['built_up_area'] > results[0]['built_up_area'], "Footprint failed to scale between smallest and largest plot"
    assert results[-1]['living_area'] > results[0]['living_area'], "Living room failed to scale"
    assert results[-1]['avg_bed'] > results[0]['avg_bed'], "Bedrooms failed to scale"

if __name__ == "__main__":
    test_land_scaling_matrix()
