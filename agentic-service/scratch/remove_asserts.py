import glob
import re

test_files = glob.glob("tests/test_*.py")

for filepath in test_files:
    with open(filepath, "r") as f:
        content = f.read()
    
    # Remove assertions on current_agent
    content = re.sub(r"^[ \t]*assert (result|updated_state|new_state|result_state|state)\.current_agent\s*==\s*[\"\'\w]+[ \t]*\n", "", content, flags=re.MULTILINE)
    
    # Remove current_agent initialization in state
    content = re.sub(r"^[ \t]*current_agent=[\"\'\w]+,\s*\n", "", content, flags=re.MULTILINE)

    # In test_execution_timeline.py, remove state.current_agent = "cost_estimation"
    content = re.sub(r"^[ \t]*state\.current_agent\s*=\s*\"cost_estimation\"\n", "", content, flags=re.MULTILINE)

    with open(filepath, "w") as f:
        f.write(content)
print("Done")
