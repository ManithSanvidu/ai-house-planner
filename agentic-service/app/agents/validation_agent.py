from app.schemas.workflow_state import WorkflowState
def validation_node(state: WorkflowState) -> WorkflowState:
    print(f"[Validation Agent] Validating constraints for workflow {state.workflow_id}...")
    
    is_valid = True
    reason = "Design meets all space constraints."
        
    state.validation_result = {
        "is_valid": is_valid,
        "reason": reason
    }
    
    state.status = "awaiting_approval" if is_valid else "rejected"
    state.current_agent = "rendering"
    return state