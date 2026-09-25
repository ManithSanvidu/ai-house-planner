from typing import Any, Literal
from uuid import UUID

from pydantic import BaseModel, Field


class ExecutionLogEntry(BaseModel):
    agent_name:str
    action:str
    tool_called:str | None=None
    duration_ms:int | None=None
    result:str
    created_at_utc:str

class CoordinatorInput(BaseModel):
    submission_id:UUID
    land_size_perches:float
    budget_lkr:float | None=None
    manual_terrain_type:str | None=None
    preferences:dict[str,Any]
    plot_constraints:dict[str, Any] | None=None
    design_seed:int | None=None
    preferred_plan_code:str | None=None
    regeneration:bool=False
    previous_base_plan_code:str | None=None
    previous_design_fingerprint:str | None=None
    region:str | None=None
    quality_level:str | None=None

class WorkflowState(BaseModel):
    workflow_id:UUID
    status:Literal["running","awaiting_approval","approved","rejected","failed","design_generated"]="running"

    #Agent results
    terrain_result:dict[str, Any] | None=None
    design_result:dict[str, Any] | None=None
    construction_plan_result:dict[str, Any] | None=None
    cost_result:dict[str, Any] | None=None
    validation_result:dict[str, Any] | None=None

    approval_status:Literal["not_requested","pending","approved","rejected","revision_requested"]="not_requested"
    retry_count:int=0
    execution_log:list[ExecutionLogEntry]=Field(default_factory=list)

    #Internal routing data
    input_data:CoordinatorInput | None=None
    current_agent:str="coordinator"

    #To store user chat feedback for revisions
    user_revision_prompt:str | None=None


