import json
path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)
for plan in data:
    if plan['designCode'] == 'HP-4B1B-2F-559':
        for c in plan['layout']['connections']:
            if 'stair' in c['from_room'] or 'stair' in c['to_room']:
                print(c)
        break
