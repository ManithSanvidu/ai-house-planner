import sys

with open("HousePlanner.API.Tests/Controllers/AiGenerationControllerTests.cs", "r") as f:
    content = f.read()

target = """var request = new AiGenerationRequest { BasePreDesignedPlanId=plan.Id,PlanSelectionMode="use","""

replacement = """var request = new AiGenerationRequest { BasePreDesignedPlanId=plan.Id,PlanSelectionMode="reference","""

if target in content:
    content = content.replace(target, replacement)
    with open("HousePlanner.API.Tests/Controllers/AiGenerationControllerTests.cs", "w") as f:
        f.write(content)
    print("Replaced successfully")
else:
    print("Target not found")
