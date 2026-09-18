from fastapi.staticfiles import StaticFiles
import os
from fastapi import FastAPI, HTTPException, Security, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import APIKeyHeader
from pydantic import BaseModel 
from uuid import UUID, uuid4
from typing import Optional, Dict, Any

from app.schemas.workflow_agent import WorkflowState, CoordinatorInput
from app.workflows.house_planning_graph import app_graph

app = FastAPI(title="Agentic AI Service - House Planner")

os.makedirs("output_plans", exist_ok=True)
app.mount("/plans", StaticFiles(directory="output_plans"), name="plans")


app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], # Allow React app
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

#Internal auth mechanism where ASP.NET can call this API
api_key_header=APIKeyHeader(name="X-Internal-API-Key")

def verify_api_key(api_key: str=Security(api_key_header)):
    if api_key!="shared-internal-secret":
        raise HTTPException(status_code=403,detail="Forbidden:Invalid API Key")
    return api_key

class StartWorkflowRequest(BaseModel):
    workflow_id:UUID
    submission_id:UUID
    budget_lkr:float
    land_size_perches:float
    manual_terrain_type:Optional[str]=None
    preferences:Dict[str,Any]

class ResumeWorkflowRequest(StartWorkflowRequest):
    revision_notes: Optional[str] = None

def execute_workflow(initial_state:WorkflowState):
    """Background task to run the LangGraph workflow"""
    print(f"Starting workflow execution for {initial_state.workflow_id}")
    final_state = app_graph.invoke(initial_state)
    import requests
    from app.config import ASPNET_API_URL, INTERNAL_API_KEY
    try:
        requests.patch(
            f"{ASPNET_API_URL}/api/v1/internal/workflows/{initial_state.workflow_id}/status",
            json={"status": final_state.get("status"), "approval_status": final_state.get("approval_status")},
            headers={"X-Internal-API-Key": INTERNAL_API_KEY, "Content-Type": "application/json"}
        )
    except Exception as e:
        print(f"Error syncing status to ASP.NET: {e}")

@app.post("/workflows/resume")
def resume_workflow(
    request:ResumeWorkflowRequest,
    background_tasks:BackgroundTasks,
    api_key:str=Security(verify_api_key)
):
    workflow_id=request.workflow_id
    initial_state=WorkflowState(
        workflow_id=workflow_id,
        status="running",
        input_data=CoordinatorInput(**request.model_dump(exclude={"revision_notes"})),
        user_revision_prompt=request.revision_notes
    )
    background_tasks.add_task(execute_workflow,initial_state)
    return{
        "message":"Workflow revision started successfully",
        "workflow_id":str(workflow_id)
    }

@app.post("/workflows/start")
def start_workflow(
    request:StartWorkflowRequest,
    background_tasks:BackgroundTasks,
    api_key:str=Security(verify_api_key)
):
    """
    Endpoint called by ASP.NET Core component after a successful intake
    """
    workflow_id=request.workflow_id

    #Construct initial state
    initial_state=WorkflowState(
        workflow_id=workflow_id,
        status="running",
        input_data=CoordinatorInput(**request.model_dump())
    )

    #To make the LangGraph response quicker it is passed to a background task so the API responds to ASP.NET Core immediately
    background_tasks.add_task(execute_workflow,initial_state)

    return{
        "message":"Workflow started successfully",
        "workflow_id":str(workflow_id)
    }

