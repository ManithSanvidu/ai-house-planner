import sys

with open("HousePlanner.API/Controllers/ConstructorWorkflowController.cs", "r") as f:
    content = f.read()

target = """            var raw = await _db.ConstructorProjectRequests.AsNoTracking()
                .Where(r => r.ConstructorId == user.Id.Value)
                .Include(r => r.Customer).Include(r => r.HouseDesign)"""

replacement = """            var raw = await _db.ConstructorProjectRequests.AsNoTracking()
                .Where(r => r.ConstructorId == user.Id.Value && r.Status == "Pending")
                .Include(r => r.Customer).Include(r => r.HouseDesign)"""

if target in content:
    content = content.replace(target, replacement)
    with open("HousePlanner.API/Controllers/ConstructorWorkflowController.cs", "w") as f:
        f.write(content)
    print("Replaced successfully")
else:
    print("Target not found")
