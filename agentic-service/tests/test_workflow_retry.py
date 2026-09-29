import pytest
import uuid
from app.workflows.house_planning_graph import route_from_validation
from app.schemas.workflow_state import WorkflowState
from app.validation.design_validation_service import validation_node
from unittest.mock import patch, MagicMock

def get_test_state():
    return WorkflowState(
        workflow_id=str(uuid.uuid4()), 
        current_agent="validation", 
        status="running"
    )

def test_valid_plan_reaches_rendering():
    state = get_test_state()
    state.validation_result = {"passed": True}
    next_node = route_from_validation(state)
    assert next_node == "rendering"

def test_invalid_plan_stops_workflow():
    state = get_test_state()
    state.validation_result = {"passed": False}
    # It hasn't been through validation_node yet, so status isn't failed.
    # But route_from_validation routes based on state.validation_result passed flag.
    # Actually route_from_validation says:
    # if state.validation_result and state.validation_result.get("passed", False): return "rendering"
    # if state.status == "failed" ... return "failed"
    # Wait, my route_from_validation modification looks like:
    # return "failed" as a fallback. Let's make sure it routes to "failed".
    next_node = route_from_validation(state)
    assert next_node == "failed"

@patch('app.validation.design_validation_service.validate_house_plan')
@patch('app.validation.design_validation_service._submit_validation_result')
def test_validation_node_sets_failure_reason(mock_submit, mock_validate):
    # Mock validate to fail
    mock_validate.return_value = MagicMock(passed=False, model_dump=lambda: {"passed": False})
    
    state = get_test_state()
    state.retry_count = 0
    
    new_state = validation_node(state)
    
    assert new_state.status == "failed"
    assert new_state.current_agent == "failed"
    assert new_state.validation_result["reason"] == "Selected catalogue plan failed validation"
    assert new_state.retry_count == 0  # No retry increment
