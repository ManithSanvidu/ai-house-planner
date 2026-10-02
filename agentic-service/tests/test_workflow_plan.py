import uuid
import pytest
from pydantic import ValidationError

from app.schemas.workflow_plan import (
    WorkflowPlan,
    WorkflowPlanStep,
    PlanStepStatus,
    create_default_house_planning_plan,
)
from app.schemas.workflow_state import WorkflowState


def test_valid_plan_parses_successfully():
    plan = create_default_house_planning_plan()
    assert len(plan.steps) == 7
    assert plan.steps[0].step_id == "S1"
    assert plan.steps[0].assigned_agent == "requirement_analysis"
    assert plan.steps[-1].step_id == "S7"


def test_invalid_agent_fails():
    with pytest.raises(ValidationError) as exc:
        WorkflowPlanStep(
            step_id="S1",
            name="Unknown",
            assigned_agent="unknown_agent"
        )
    assert "Invalid assigned_agent" in str(exc.value)


def test_duplicate_step_ids_fails():
    with pytest.raises(ValidationError) as exc:
        WorkflowPlan(
            objective="test",
            steps=[
                WorkflowPlanStep(step_id="S1", name="Step 1", assigned_agent="design"),
                WorkflowPlanStep(step_id="S1", name="Step 2", assigned_agent="design"),
            ]
        )
    assert "Step IDs must be unique" in str(exc.value)


def test_unknown_dependency_fails():
    with pytest.raises(ValidationError) as exc:
        WorkflowPlan(
            objective="test",
            steps=[
                WorkflowPlanStep(step_id="S1", name="Step 1", assigned_agent="design", dependencies=["S99"]),
            ]
        )
    assert "does not exist" in str(exc.value)


def test_self_dependency_fails():
    with pytest.raises(ValidationError) as exc:
        WorkflowPlan(
            objective="test",
            steps=[
                WorkflowPlanStep(step_id="S1", name="Step 1", assigned_agent="design", dependencies=["S1"]),
            ]
        )
    assert "cannot depend on itself" in str(exc.value)


def test_circular_dependency_fails():
    with pytest.raises(ValidationError) as exc:
        WorkflowPlan(
            objective="test",
            steps=[
                WorkflowPlanStep(step_id="S1", name="Step 1", assigned_agent="design", dependencies=["S3"]),
                WorkflowPlanStep(step_id="S2", name="Step 2", assigned_agent="design", dependencies=["S1"]),
                WorkflowPlanStep(step_id="S3", name="Step 3", assigned_agent="design", dependencies=["S2"]),
            ]
        )
    assert "Circular dependencies detected" in str(exc.value)


def test_backward_compatibility():
    # Should be able to create WorkflowState without providing a plan
    state = WorkflowState(workflow_id=uuid.uuid4())
    assert state.plan is None
    assert state.current_step_id is None
    assert state.completed_step_ids == []


def test_safe_defaults():
    # completed_step_ids must not be shared between instances
    state1 = WorkflowState(workflow_id=uuid.uuid4())
    state2 = WorkflowState(workflow_id=uuid.uuid4())
    
    state1.completed_step_ids.append("S1")
    
    assert "S1" in state1.completed_step_ids
    assert "S1" not in state2.completed_step_ids

