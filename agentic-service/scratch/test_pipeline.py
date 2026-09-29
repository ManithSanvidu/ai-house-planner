import os
import json
from app.agents.design_agent import design_node
from app.schemas.workflow_state import WorkflowState, CoordinatorInput

def test_full_pipeline(beds, baths, floors):
    print(f"\n--- Testing {beds} Beds / {baths} Baths / {floors} Floors ---")
    state = WorkflowState(
        workflow_id="12345678-1234-1234-1234-123456789012",
        status="running",
        current_agent="coordinator",
        approval_status="not_requested",
        terrain_result={"terrain_type": "flat", "notable_features": []},
        land_size=20.0,
        input_data=CoordinatorInput(
            submission_id="12345678-1234-1234-1234-123456789012",
            land_size_perches=20.0,
            preferences={"bedrooms": beds, "bathrooms": baths, "floors": floors, "style": "Modern Tropical"},
            plot_constraints={"plot_width_ft": 40, "plot_length_ft": 40} # Small plot to trigger failure
        )
    )
    
    import app.agents.design_agent as da
    da._submit_design = lambda state: "success"
    
    new_state = design_node(state)
    
    print(f"Status: {new_state.status}")
    if new_state.design_result:
        rooms = new_state.design_result.get('rooms', [])
        print(f"Rooms generated: {len(rooms)}")
        if rooms:
            print(f"Sample room coordinates: {rooms[0].get('room_type')} at ({rooms[0].get('x')}, {rooms[0].get('y')})")
    else:
        print("Design generation failed.")
        print(new_state.validation_result)

if __name__ == "__main__":
    test_full_pipeline(3, 2, 1)
    test_full_pipeline(5, 3, 2)
