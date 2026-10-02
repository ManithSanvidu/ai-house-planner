import uuid
from fastapi import BackgroundTasks
from app.api.routers.workflow_routes import resume_workflow, ResumeWorkflowRequest
from app.schemas.workflow_plan import PlanStepStatus
from app.schemas.workflow_state import WorkflowState

class MockBackgroundTasks(BackgroundTasks):
    def __init__(self):
        super().__init__()
        self.tasks = []
    def add_task(self, func, *args, **kwargs):
        self.tasks.append((func, args, kwargs))

def test_resume_creates_plan_with_s1_s2_complete():
    # Test E: Resume creates plan with S1/S2 complete
    # Test F: Revision data preserved
    req = ResumeWorkflowRequest(
        workflow_id=uuid.uuid4(),
        resume_from="design",
        user_revision_prompt="Make the kitchen bigger",
        land_size_perches=15.0,
        bedrooms=3,
        house_type="tropical",
        terrain_result={"terrain_type": "hillside"},
        previous_design={"rooms": []},
        preferences={"bathrooms": 2},
    )
    
    bg_tasks = MockBackgroundTasks()
    
    res = resume_workflow(request=req, background_tasks=bg_tasks, api_key="test")
    assert res["message"] == "Workflow resumed successfully"
    
    # Extract the state passed to execute_workflow
    func, args, kwargs = bg_tasks.tasks[0]
    state: WorkflowState = args[0]
    
    # Test E Asserts
    assert state.plan is not None
    assert state.plan.steps[0].step_id == "S1"
    assert state.plan.steps[0].status == PlanStepStatus.COMPLETED
    assert state.plan.steps[1].step_id == "S2"
    assert state.plan.steps[1].status == PlanStepStatus.COMPLETED
    
    for i in range(2, 7):
        assert state.plan.steps[i].status == PlanStepStatus.PENDING
        
    assert state.completed_step_ids == ["S1", "S2"]
    assert state.current_step_id is None
    
    # Test F Asserts (Revision data preserved)
    assert state.terrain_result == {"terrain_type": "hillside"}
    assert state.design_result == {"rooms": []}
    assert state.user_revision_prompt == "Make the kitchen bigger"
