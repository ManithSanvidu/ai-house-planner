from __future__ import annotations
from langgraph.graph import END, StateGraph

from app.services.construction_planning_service import construction_planning_node
from app.orchestration.workflow_router import coordinator_node, is_agent_failed
from app.services.cost_estimation_service import cost_estimation_node
from app.agents.design_agent import design_node
from app.agents.land_analysis_agent import land_analysis_node
from app.agents.requirement_analysis_agent import requirement_analysis_node
from app.services.rendering_service import rendering_node
from app.design.visualization.visualization_agent import visualization_node
from app.validation.design_validation_service import validation_node
from app.schemas.workflow_state import WorkflowState

def route_from_coordinator(state: WorkflowState) -> str:
    """Plan-driven dynamic router."""
    if is_agent_failed(state):
        return END
        
    # Check if a step is currently running
    if state.current_step_id is not None and state.plan:
        current_step = next((s for s in state.plan.steps if s.step_id == state.current_step_id), None)
        if current_step and current_step.assigned_agent:
            return current_step.assigned_agent
            
    # If no step is running and we didn't fail, we are done with the plan. Route to rendering.
    return "rendering"


def route_after_cost_estimation(state: WorkflowState) -> str:
    """Stop when cost calculation or persistence fails."""
    return "failed" if state.status == "failed" or getattr(state, "current_agent", None) == "failed" else "validation"


workflow = StateGraph(WorkflowState)
workflow.add_node("coordinator", coordinator_node)
workflow.add_node("requirement_analysis", requirement_analysis_node)
workflow.add_node("land_analysis", land_analysis_node)
workflow.add_node("design", design_node)
workflow.add_node("construction_planning", construction_planning_node)
workflow.add_node("cost_estimation", cost_estimation_node)
workflow.add_node("validation", validation_node)
workflow.add_node("rendering", rendering_node)
workflow.add_node("visualization", visualization_node)

workflow.set_entry_point("coordinator")

# Every plan agent returns to coordinator
plan_agents = [
    "requirement_analysis",
    "land_analysis",
    "design",
    "visualization",
    "construction_planning",
    "cost_estimation",
    "validation"
]

for agent in plan_agents:
    workflow.add_edge(agent, "coordinator")

# Coordinator routes to the next agent, rendering, or END
workflow.add_conditional_edges(
    "coordinator",
    route_from_coordinator,
    {
        "requirement_analysis": "requirement_analysis",
        "land_analysis": "land_analysis",
        "design": "design",
        "visualization": "visualization",
        "construction_planning": "construction_planning",
        "cost_estimation": "cost_estimation",
        "validation": "validation",
        "rendering": "rendering",
        END: END
    }
)

workflow.add_edge("rendering", END)

app_graph = workflow.compile()

# A selected pre-designed plan already has persisted geometry and a construction
# plan. It enters Component C directly, then follows the same validation gate.
pre_designed_workflow = StateGraph(WorkflowState)
pre_designed_workflow.add_node("cost_estimation", cost_estimation_node)
pre_designed_workflow.add_node("validation", validation_node)
pre_designed_workflow.set_entry_point("cost_estimation")
pre_designed_workflow.add_conditional_edges(
    "cost_estimation", route_after_cost_estimation,
    {"failed": END, "validation": "validation"},
)
pre_designed_workflow.add_edge("validation", END)
pre_designed_graph = pre_designed_workflow.compile()
