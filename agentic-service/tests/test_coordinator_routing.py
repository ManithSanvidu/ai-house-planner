import uuid
import pytest
from app.schemas.workflow_state import WorkflowState, CoordinatorInput
from app.schemas.workflow_plan import create_default_house_planning_plan, PlanStepStatus
from app.orchestration.workflow_router import coordinator_node

def test_new_workflow_sequence():
    # Test A: New workflow sequence
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        )
    )
    
    # 1. Start -> coordinator -> requirement_analysis
    state = coordinator_node(state)
    assert state.plan is not None
    assert state.current_step_id == "S1"
    assert state.plan.steps[0].status == PlanStepStatus.RUNNING
    
    # 2. Return from S1
    state = coordinator_node(state)
    assert state.plan.steps[0].status == PlanStepStatus.COMPLETED
    assert "S1" in state.completed_step_ids
    assert state.current_step_id == "S2"
    assert state.plan.steps[1].status == PlanStepStatus.RUNNING

def test_revision_sequence():
    # Test B: Revision sequence
    plan = create_default_house_planning_plan("Test")
    for step in plan.steps:
        if step.step_id in ["S1", "S2"]:
            step.status = PlanStepStatus.COMPLETED
    
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        ),
        plan=plan,
        completed_step_ids=["S1", "S2"],
        current_step_id=None
    )
    
    # Coordinator should select S3 (Design)
    state = coordinator_node(state)
    assert state.current_step_id == "S3"
    assert state.plan.steps[2].status == PlanStepStatus.RUNNING

def test_step_status_transitions():
    # Test C: Step status transitions
    # Test D: Completed IDs synchronized
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        )
    )
    
    state = coordinator_node(state)
    assert state.plan.steps[0].status == PlanStepStatus.RUNNING
    
    state = coordinator_node(state)
    assert state.plan.steps[0].status == PlanStepStatus.COMPLETED
    assert state.completed_step_ids == ["S1"]
    
    state = coordinator_node(state)
    assert state.plan.steps[1].status == PlanStepStatus.COMPLETED
    assert state.completed_step_ids == ["S1", "S2"]
    # Verify no duplicates
    assert len(state.completed_step_ids) == 2

def test_dependency_enforcement():
    # Test E: Dependency enforcement
    plan = create_default_house_planning_plan("Test")
    # S3 depends on S1 and S2.
    # We will mark S1 as COMPLETED, S2 as PENDING. S3 is PENDING.
    # S3 cannot be selected because S2 is not COMPLETED.
    plan.steps[0].status = PlanStepStatus.COMPLETED
    
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        ),
        plan=plan,
        completed_step_ids=["S1"]
    )
    
    state = coordinator_node(state)
    # S2 should be selected, NOT S3.
    assert state.current_step_id == "S2"

def test_failed_agent_behavior():
    # Test F: Failed agent behavior
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        )
    )
    state = coordinator_node(state)
    assert state.current_step_id == "S1"
    
    # Simulate agent failure
    state.status = "failed"
    state = coordinator_node(state)
    
    assert state.plan.steps[0].status == PlanStepStatus.FAILED
    assert state.status == "failed"
    assert "S1" not in state.completed_step_ids
    assert state.current_step_id is None

def test_plan_completion():
    # Test G: Plan completion
    plan = create_default_house_planning_plan("Test")
    for step in plan.steps:
        step.status = PlanStepStatus.COMPLETED
    
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        ),
        plan=plan,
        completed_step_ids=[s.step_id for s in plan.steps]
    )
    
    state = coordinator_node(state)
    assert state.current_step_id is None
    # No further step selected

def test_existing_plan_preserved():
    # Test H: Existing plan preserved
    plan = create_default_house_planning_plan("Custom")
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        ),
        plan=plan
    )
    state = coordinator_node(state)
    assert state.plan.objective == "Custom"

def test_no_current_agent_dependency():
    # Test I: No current_agent dependency
    state = WorkflowState(
        workflow_id=uuid.uuid4(),
        input_data=CoordinatorInput(
            submission_id=uuid.uuid4(),
            land_size_category="medium",
            land_size_perches=10.0,
        )
    )
    # Setting current_agent to something should not affect coordinator routing
    state.current_agent = "garbage_agent"
    state = coordinator_node(state)
    assert state.current_step_id == "S1"
