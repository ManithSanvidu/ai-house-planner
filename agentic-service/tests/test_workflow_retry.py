import pytest
import uuid
from app.orchestration.workflow_router import coordinator_node
from app.schemas.workflow_state import WorkflowState
from app.schemas.workflow_plan import create_default_house_planning_plan, PlanStepStatus
from app.validation.design_validation_service import validation_node
from unittest.mock import patch, MagicMock

def get_test_state():
    return WorkflowState(
        workflow_id=str(uuid.uuid4()), 
        status="running"
    )

def test_valid_plan_reaches_rendering():
    state = get_test_state()
    plan = create_default_house_planning_plan("Test")
    for step in plan.steps:
        step.status = PlanStepStatus.COMPLETED
    state.plan = plan
    state.completed_step_ids = [s.step_id for s in plan.steps]
    state.validation_result = {"passed": True}
    
    # Coordinator routes to rendering when plan is finished
    from app.workflows.house_planning_graph import route_from_coordinator
    next_node = route_from_coordinator(state)
    assert next_node == "rendering"

def test_invalid_plan_stops_workflow():
    state = get_test_state()
    state.status = "failed"
    from app.workflows.house_planning_graph import route_from_coordinator
    from langgraph.graph import END
    next_node = route_from_coordinator(state)
    assert next_node == END

@patch('app.validation.design_validation_service.validate_house_plan')
@patch('app.validation.design_validation_service._submit_validation_result')
def test_validation_node_sets_failure_reason(mock_submit, mock_validate):
    # Mock validate to fail
    mock_validate.return_value = MagicMock(passed=False, model_dump=lambda: {"passed": False})
    
    state = get_test_state()
    state.retry_count = 0
    
    new_state = validation_node(state)
    
    assert new_state.status == "failed"
    assert new_state.validation_result["reason"] == "Selected catalogue plan failed validation"
    assert new_state.retry_count == 0  # No retry increment
