import pytest
from app.design.generation.generation_service import generate_layout
from app.design.catalogue.base_plan_library import load_base_plan_catalog
from app.design.generation.diversity import geometry_fingerprint

def test_generate_layout_is_strictly_deterministic_and_immutable():
    """
    Ensure generate_layout selects a base plan and returns its geometry unaltered.
    """
    # 1. Fetch catalog
    catalog = load_base_plan_catalog()
    assert len(catalog) > 0, "Catalog should not be empty"
    
    # 2. Pick any plan from catalog to test compatibility against
    target_plan = catalog[0]
    
    # Run layout generation with preferences that should match target_plan
    result = generate_layout(
        land_size_perches=target_plan.maximum_land_perches or target_plan.minimum_land_perches + 5.0,
        terrain_type=target_plan.supported_terrains[0],
        preferences={
            'bedrooms': target_plan.bedrooms,
            'floors': target_plan.floors
        },
        plot_constraints=None,
        preferred_plan_code=target_plan.plan_code
    )
    
    # 3. Assert plan code matches
    assert result.candidate_summary.get('selected_plan_code') == target_plan.plan_code
    assert result.candidate_summary.get('generation_mode') == 'deterministic_template_selection'
    
    # 4. Assert fingerprint matches exactly
    original_fingerprint = geometry_fingerprint(target_plan.design)
    assert result.geometry_fingerprint == original_fingerprint, "Fingerprint mutated during generation!"
    
    # 5. Assert coordinates are strictly identical (No PlanAdapter mutations)
    assert len(result.rooms) == len(target_plan.design.rooms)
    for res_room, orig_room in zip(
        sorted(result.rooms, key=lambda r: r.room_id),
        sorted(target_plan.design.rooms, key=lambda r: r.room_id)
    ):
        assert res_room.x == orig_room.x, f"Room {res_room.room_id} X coordinate mutated"
        assert res_room.y == orig_room.y, f"Room {res_room.room_id} Y coordinate mutated"
        assert res_room.width == orig_room.width, f"Room {res_room.room_id} width mutated"
        assert res_room.length == orig_room.length, f"Room {res_room.room_id} length mutated"
