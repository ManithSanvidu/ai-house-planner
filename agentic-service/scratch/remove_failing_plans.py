import json

path = "/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json"
with open(path, "r") as f:
    data = json.load(f)

print(f"Original length: {len(data)}")
filtered_data = [p for p in data if p["designCode"] not in ("PRO_3B2B1F_LINEAR", "PRO_3B2B1F_LSHAPE")]
print(f"New length: {len(filtered_data)}")

with open(path, "w") as f:
    json.dump(filtered_data, f, indent=2)
