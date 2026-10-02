import os
import re

files = [
    "app/validation/design_validation_service.py",
    "app/orchestration/workflow_router.py",
    "app/services/construction_planning_service.py",
    "app/services/cost_estimation_service.py",
    "app/agents/design_agent.py",
    "app/agents/land_analysis_agent.py"
]

for filepath in files:
    with open(filepath, "r") as f:
        content = f.read()
    
    # Remove state.current_agent = "..."
    content = re.sub(r"^[ \t]*state\.current_agent\s*=\s*[\"\'\w]+[ \t]*\n", "", content, flags=re.MULTILINE)
    
    with open(filepath, "w") as f:
        f.write(content)
print("Done")
