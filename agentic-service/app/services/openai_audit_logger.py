import logging
import json
import os
from datetime import datetime, timezone
import inspect

# Configure a dedicated logger for AI calls
audit_logger = logging.getLogger("openai_audit")
audit_logger.setLevel(logging.INFO)

# Make sure the logs directory exists
log_dir = os.path.join(os.path.dirname(__file__), "..", "..", "logs")
os.makedirs(log_dir, exist_ok=True)

file_handler = logging.FileHandler(os.path.join(log_dir, "openai_audit.log"))
file_handler.setFormatter(logging.Formatter('%(message)s'))
audit_logger.addHandler(file_handler)

def log_ai_call(
    workflow_id: str | None,
    purpose: str,
    model: str,
    input_tokens: int,
    output_tokens: int
):
    """
    Logs an AI call for forensic auditing purposes.
    Automatically captures the caller file and function.
    """
    # Inspect the stack to find the caller
    stack = inspect.stack()
    caller_file = "unknown"
    caller_function = "unknown"
    
    # stack[0] is this function, stack[1] is the caller
    if len(stack) > 1:
        frame = stack[1]
        caller_file = os.path.basename(frame.filename)
        caller_function = frame.function

    log_entry = {
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "workflow_id": str(workflow_id) if workflow_id else None,
        "purpose": purpose,
        "model": model,
        "input_tokens": input_tokens,
        "output_tokens": output_tokens,
        "caller_file": caller_file,
        "caller_function": caller_function
    }
    
    audit_logger.info(json.dumps(log_entry))
    
    # Also print to stdout for immediate visibility in docker logs
    print(f"[AUDIT LOG] {json.dumps(log_entry)}")
