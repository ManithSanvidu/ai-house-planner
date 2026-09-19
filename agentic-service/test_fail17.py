import json

path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    for r in plan['layout']['rooms']:
        if r.get('doors'):
            print("Found doors in", plan['designCode'])
            break
