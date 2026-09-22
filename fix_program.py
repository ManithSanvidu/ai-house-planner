import sys

filepath = "HousePlanner.API/Program.cs"
with open(filepath, 'r') as f:
    content = f.read()

target1 = "builder.Services.AddScoped<IConstructorWorkflowService, ConstructorWorkflowService>();"
replacement1 = """builder.Services.AddScoped<IConstructorWorkflowService, ConstructorWorkflowService>();
builder.Services.AddScoped<IDailyConstructionLogService, DailyConstructionLogService>();"""

if target1 in content:
    content = content.replace(target1, replacement1)
    with open(filepath, 'w') as f:
        f.write(content)
    print("Program.cs updated")
else:
    print("Target not found!")
