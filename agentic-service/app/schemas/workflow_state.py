from pydantic import BaseModel, Field
from typing import Optional,List,Literal,Dict,Any
from uuid import UUID

class ExecutionLogEntry(BaseModel):
    agent_name:str
    action:str
    tool_called:Optional[str]=None
    duration_ms:Optional[int]=None
    result:str
    created_at_utc:str

class CoordinatorInput(BaseModel):
    submission_id:UUID
    land_size_perches:float
    budget_lkr:Optional[float]=None
    manual_terrain_type:Optional[str]=None
    preferences:Dict[str,Any]
    plot_constraints:Optional[Dict[str,Any]]=None
    design_seed:Optional[int]=None
    preferred_plan_code:Optional[str]=None
    regeneration:bool=False
    previous_base_plan_code:Optional[str]=None
    previous_design_fingerprint:Optional[str]=None

class WorkflowState(BaseModel):
    workflow_id:UUID
    status:Literal["running","awaiting_approval","approved","rejected","failed","design_generated"]="running"

    #Agent results
    terrain_result:Optional[Dict[str,Any]]=None
    design_result:Optional[Dict[str,Any]]=None
    construction_plan_result:Optional[Dict[str,Any]]=None
    cost_result:Optional[Dict[str,Any]]=None
    validation_result:Optional[Dict[str,Any]]=None

    approval_status:Literal["not_requested","pending","approved","rejected","revision_requested"]="not_requested"
    retry_count:int=0
    execution_log:List[ExecutionLogEntry]=Field(default_factory=list)

    #Internal routing data
    input_data:Optional[CoordinatorInput]=None
    current_agent:str="coordinator"

    #To store user chat feedback for revisions
    user_revision_prompt:Optional[str]=None


