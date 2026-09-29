from unittest.mock import MagicMock, patch

from app.schemas.workflow_state import WorkflowState
from app.agents.design_agent import design_node
from app.services.construction_planning_service import construction_planning_node
from app.services.cost_estimation_service import cost_estimation_node
from app.services.ai_guard import execute_once, reset_ai_guard


def test_design_node_guard_skips_duplicate_ai_generation():
    state = WorkflowState(
        workflow_id="11111111-1111-1111-1111-111111111111",
        ai_design_generated=True,
        design_version=1,
        design_result={"rooms": []},
        current_agent="design",
    )
    result = design_node(state)

    assert result.current_agent == "cost_estimation"


def test_construction_guard_reuses_existing_plan_without_persisting():
    state = WorkflowState(
        workflow_id="11111111-1111-1111-1111-111111111111",
        construction_plan_result={"phases": [{"name": "Foundation"}]},
        current_agent="construction_planning",
    )
    with patch("app.services.construction_planning_service.requests.patch") as persist:
        result = construction_planning_node(state)

    persist.assert_not_called()
    assert result.current_agent == "cost_estimation"


def test_cost_guard_reuses_existing_estimate_without_ai_or_persistence():
    state = WorkflowState(
        workflow_id="11111111-1111-1111-1111-111111111111",
        cost_result={"total_cost_lkr": 100_000},
        current_agent="cost_estimation",
    )
    with patch("app.services.cost_estimation_service._run_estimation") as estimate, \
         patch("app.services.cost_estimation_service._persist_cost_estimate") as persist:
        result = cost_estimation_node(state)

    estimate.assert_not_called()
    persist.assert_not_called()
    assert result.current_agent == "validation"


def test_ai_guard_reuses_one_successful_call_per_workflow_and_purpose(capsys):
    reset_ai_guard()
    operation = MagicMock(return_value={"result": "cached"})

    first, first_blocked = execute_once("workflow-guard", "design_strategy", operation)
    second, second_blocked = execute_once("workflow-guard", "design_strategy", operation)

    assert first == {"result": "cached"}
    assert first_blocked is False
    assert second_blocked is True
    operation.assert_called_once()
    assert "[AI GUARD DB] CACHE HIT" in capsys.readouterr().out

