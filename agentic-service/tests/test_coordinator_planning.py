import uuid
import pytest
from app.schemas.workflow_state import WorkflowState, CoordinatorInput
from app.schemas.workflow_plan import PlanStepStatus, create_default_house_planning_plan
from app.orchestration.workflow_router import coordinator_node

def test_new_workflow_coordinator_creates_plan():
    # Test A: New workflow Coordinator creates plan
    workflow_id = uuid.uuid4()
    state = WorkflowState(
        workflow_id=workflow_id,
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
            bedrooms=4,
            bathrooms=2,
            budget_lkr=15000000.0,
        )
    )
    
    assert state.plan is None
    
    new_state = coordinator_node(state)
    
    assert new_state.plan is not None
    assert len(new_state.plan.steps) == 7
    assert new_state.plan.steps[0].status == PlanStepStatus.RUNNING
    assert all(step.status == PlanStepStatus.PENDING for step in new_state.plan.steps[1:])
    assert new_state.completed_step_ids == []

    # Test C: Structured fallback objective
    # Based on input_data: bedrooms=4, bathrooms=2, budget_lkr=15M
    assert "Design a 4-bedroom, 2-bathroom house within a budget of LKR 15,000,000.00." == new_state.plan.objective
    
    # Test G: Planning log created once
    plan_logs = [log for log in new_state.execution_log if log.action == "workflow_plan_created"]
    assert len(plan_logs) == 1

def test_objective_uses_natural_language_prompt():
    # Test B: Objective uses natural-language prompt
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
            natural_language_prompt="   A beautiful modern home by the beach.   "
        )
    )
    
    new_state = coordinator_node(state)
    assert new_state.plan.objective == "A beautiful modern home by the beach."

def test_existing_plan_preserved():
    # Test D: Existing plan preserved
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        ),
        plan=create_default_house_planning_plan("Custom existing objective"),
        completed_step_ids=["S1"]
    )
    state.plan.steps[0].status = PlanStepStatus.COMPLETED
    
    new_state = coordinator_node(state)
    
    assert new_state.plan.objective == "Custom existing objective"
    assert new_state.plan.steps[0].status == PlanStepStatus.COMPLETED
    assert new_state.completed_step_ids == ["S1"]
    
    # Verify no new creation log added
    plan_logs = [log for log in new_state.execution_log if log.action == "workflow_plan_created"]
    assert len(plan_logs) == 0

