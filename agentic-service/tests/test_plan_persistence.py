import uuid
import pytest
from unittest.mock import patch, MagicMock
from app.utils.plan_persistence import persist_workflow_plan_state, PlanPersistenceError
from app.schemas.workflow_state import WorkflowState, CoordinatorInput
from app.schemas.workflow_plan import create_default_house_planning_plan

@patch("app.utils.plan_persistence.httpx.Client")
def test_persist_workflow_plan_state_success(mock_client_class):
    mock_client = MagicMock()
    mock_response = MagicMock()
    mock_response.raise_for_status.return_value = None
    mock_client.patch.return_value = mock_response
    mock_client_class.return_value.__enter__.return_value = mock_client

    plan = create_default_house_planning_plan("Test")
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        ),
        plan=plan,
        current_step_id="S1",
        completed_step_ids=[]
    )

    persist_workflow_plan_state(state)
    mock_client.patch.assert_called_once()
    args, kwargs = mock_client.patch.call_args
    assert "json" in kwargs
    assert kwargs["json"]["currentStepId"] == "S1"
    assert kwargs["json"]["completedStepIds"] == []
    assert "plan" in kwargs["json"]

@patch("app.utils.plan_persistence.httpx.Client")
def test_persist_workflow_plan_state_failure(mock_client_class):
    mock_client = MagicMock()
    mock_response = MagicMock()
    mock_response.raise_for_status.side_effect = Exception("HTTP Error")
    mock_client.patch.return_value = mock_response
    mock_client_class.return_value.__enter__.return_value = mock_client

    plan = create_default_house_planning_plan("Test")
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        ),
        plan=plan
    )

    with pytest.raises(PlanPersistenceError):
        persist_workflow_plan_state(state)
    
    # Check retries occurred (2 attempts)
    assert mock_client.patch.call_count == 2
