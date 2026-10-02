import asyncio
from uuid import uuid4
from app.schemas.workflow_state import WorkflowState, CoordinatorInput
from app.services.construction_planning_service import construction_planning_node
import os
os.environ["ASPNET_API_URL"] = "http://localhost:5265/api/v1"

state = WorkflowState(
    workflow_id=uuid4(),
    input_data=CoordinatorInput(
        submission_id=uuid4(),
        land_size_category="small",
        land_size_perches=10,
        bedrooms=3,
        bathrooms=2,
        house_type="modern",
        target_duration_days=120
    )
)

state = construction_planning_node(state)
print("Construction plan result:")
print(state.construction_plan_result)
print("Execution log:")
for log in state.execution_log:
    print(log.action, log.result)
