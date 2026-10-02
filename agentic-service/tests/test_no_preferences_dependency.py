import pytest
from uuid import uuid4
from datetime import datetime, timezone
from app.schemas.workflow_state import WorkflowState, CoordinatorInput
from app.services.construction_planning_service import construction_planning_node

def test_construction_planning_no_preferences():
    # Setup state
    input_data = CoordinatorInput(
        submission_id=uuid4(),
        land_size_category="small",
        land_size_perches=10.0,
        bedrooms=3,
        bathrooms=1,
        house_type="modern"
    )
    
    state = WorkflowState(
        workflow_id=uuid4(),
        input_data=input_data,
        terrain_result={"terrain_type": "flat"},
        design_result={
            "floor_count": 1,
            "total_built_up_area_sqft": 1500,
            "rooms": [
                {"room_type": "bath"}
            ]
        }
    )
    
    # Execute
    result_state = construction_planning_node(state)
    
    # Assertions
    assert result_state.construction_plan_result is not None
    assert "phases" in result_state.construction_plan_result
    assert "project_summary" in result_state.construction_plan_result
