import pytest
from app.design.program.models import DesignGenerationInput
from app.design.generation.generation_service import prepare_inputs
from app.design.generation.spatial_planner import plan_spatial_program

def test_design_generation_input_contract():
    # 1. Provide only DesignGenerationInput (no legacy dicts)
    input_data = DesignGenerationInput(
        land_size_perches=20,
        bedrooms=3,
        bathrooms=2,
        house_type="modern",
        terrain_type="flat"
    )

    # 2. Prepare inputs using only input_data
    req, plot = prepare_inputs(
        land_size_perches=input_data.land_size_perches,
        terrain_type=input_data.terrain_type,
        input_data=input_data
    )

    # 3. Plan spatial program
    program, _ = plan_spatial_program(req, plot)

    # 4. Verify the generated rooms match deterministic requirements exactly
    gen_beds = sum(1 for room in program.rooms if "bedroom" in room.type.lower())
    gen_baths = sum(1 for room in program.rooms if "bath" in room.type.lower())
    
    # Expected standard additional spaces based on the deterministic planner
    expected_additional = {"living_room", "kitchen", "dining_area"}
    gen_additional = {room.type.lower() for room in program.rooms if "bedroom" not in room.type.lower() and "bath" not in room.type.lower()}

    # Assert correct structural counts
    assert gen_beds == 3, f"Expected 3 bedrooms, got {gen_beds}"
    assert gen_baths == 2, f"Expected 2 bathrooms, got {gen_baths}"
    
    # Assert additional spaces are strictly the expected deterministic set, no heuristics added
    assert gen_additional == expected_additional, f"Expected additional rooms {expected_additional}, got {gen_additional}"
