import json

path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    if plan['designCode'].startswith('HP-4B'):
        layout = plan['layout']
        for r in layout['rooms']:
            if 'stair' in r['room_id'] and r['length'] == 7.5:
                r['length'] = 10.0
                
            if r['room_id'] in ('bed_1', 'bed_2', 'study_4', 'study_5', 'bed_3', 'bed_4', 'bed_5'):
                if r['y'] == 13 and r['length'] == 16.5:
                    r['length'] = 19.0

with open(path, 'w') as f:
    json.dump(data, f, indent=2)
print("Patched stairs to 10")
