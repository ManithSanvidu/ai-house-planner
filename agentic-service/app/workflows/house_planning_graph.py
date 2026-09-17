from langgraph.graph import StateGraph, END
from app.schemas.workflow_state import WorkflowState
from app.agents.coordinator_agent import coordinator_node
from app.agents.land_analysis_agent import land_analysis_node
from app.agents.design_agent import design_node
from app.agents.cost_estimation_agent import cost_estimation_node
from app.agents.validation_agent import validation_node
from app.agents.rendering_agent import rendering_node

# Maximum allowed revision/retry attempts upon validation failure
MAX_VALIDATION_RETRIES = 3

def route_from_coordinator(state: WorkflowState) -> str:
    """Conditional edge router from the Coordinator"""
    if state.current_agent in ["land_analysis", "design"]:
        return state.current_agent
    if state.input_data and state.input_data.manual_terrain_type:
        return "design"
    return "land_analysis"

def route_from_validation(state: WorkflowState) -> str:
    """Conditional edge router from the Validation Agent based on pass/fail and retry limit"""
    if state.current_agent == "rendering" or state.status == "awaiting_approval":
        return "rendering"
    elif state.current_agent == "design":
        return "revision_to_design"
    else:
        return "failed"

# Initialize the State Graph
workflow = StateGraph(WorkflowState)

# Add Nodes
workflow.add_node("coordinator", coordinator_node)
workflow.add_node("land_analysis", land_analysis_node)
workflow.add_node("design", design_node)
workflow.add_node("cost_estimation", cost_estimation_node)
workflow.add_node("validation", validation_node)
workflow.add_node("rendering", rendering_node)

workflow.set_entry_point("coordinator")

# Add Edges
workflow.add_conditional_edges(
    "coordinator",
    route_from_coordinator,
    {
        "land_analysis": "land_analysis",
        "design": "design",
    }
)

workflow.add_edge("land_analysis", "design")
workflow.add_edge("design", "cost_estimation")
workflow.add_edge("cost_estimation", "validation")

# Validation routes conditionally: to rendering (if passed), back to design (revision), or END (failed)
workflow.add_conditional_edges(
    "validation",
    route_from_validation,
    {
        "rendering": "rendering",
        "revision_to_design": "design",
        "failed": END,
    }
)

workflow.add_edge("rendering", END)

app_graph = workflow.compile()
