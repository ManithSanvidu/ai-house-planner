import json
path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f: data = json.load(f)
for plan in data:
    if plan['designCode'] == 'HP-4B1B-2F-559':
        for r in plan['layout']['rooms']:
            if 'stair' in r['room_id']:
                print(r['room_id'], r['width'], r['length'], r['y'])
