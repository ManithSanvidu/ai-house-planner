from __future__ import annotations
from datetime import datetime, timezone
from app.schemas.workflow_state import ExecutionLogEntry, WorkflowState


def coordinator_node(state: WorkflowState) -> WorkflowState:
    """
    The Coordinator Agent acts as the entry point and air traffic controller.
    """
    input_data = state.input_data
    print(f"[Coordinator Agent] Processing submission: {input_data.submission_id}")

    # Route to Land Analysis always
    next_agent = "land_analysis"
    action_log = "Routed to Land Analysis."

    state.current_agent = next_agent
    print(f"[Coordinator Agent] {action_log}")

    state.execution_log.append(ExecutionLogEntry(
        agent_name="CoordinatorAgent",
        action=f"Workflow initialised — {action_log}",
        result=f"next_agent={next_agent}",
        created_at_utc=datetime.now(timezone.utc).isoformat(),
    ))

    return state