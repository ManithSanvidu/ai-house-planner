import pytest
from app.design.generation.spatial_planner import plan_spatial_program
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.program.models import Requirements

def test_no_legacy_generation_fields():
    # Input
    req = Requirements(
        bedrooms=3,
        bathrooms=2,
        floors=1,
        style="modern" # which maps to house_type in generation_service
    )
    plot = PlotConstraints(plot_width_ft=100, plot_length_ft=100, land_size_perches=10.0)

    # Act
    program, _ = plan_spatial_program(req, plot)

    # Assert
    gen_beds = sum(1 for room in program.rooms if "bedroom" in room.type.lower())
    gen_baths = sum(1 for room in program.rooms if "bath" in room.type.lower())

    assert gen_beds == 3, f"Expected 3 bedrooms, got {gen_beds}"
    assert gen_baths == 2, f"Expected 2 bathrooms, got {gen_baths}"

    # Also run the full node if possible? Let's verify via the full agent.
    from uuid import uuid4
    from app.schemas.workflow_state import WorkflowState, CoordinatorInput
    from app.agents.design_agent import design_node
    
    input_data = CoordinatorInput(
        submission_id=uuid4(),
        land_size_category="small",
        land_size_perches=10.0,
        bedrooms=3,
        bathrooms=2,
        house_type="modern"
    )
    
    state = WorkflowState(
        workflow_id=uuid4(),
        input_data=input_data
    )
    
    result_state = design_node(state)
    
    assert result_state.status != "failed", "Design generation failed"
    assert result_state.design_result is not None, "No design result generated"
    
    rooms = result_state.design_result.get("rooms", [])
    beds = sum(1 for r in rooms if "bedroom" in r.get("room_type", "").lower())
    baths = sum(1 for r in rooms if "bath" in r.get("room_type", "").lower())
    
    assert beds == 3, f"Full agent expected 3 bedrooms, got {beds}"
    assert baths == 2, f"Full agent expected 2 bathrooms, got {baths}"
