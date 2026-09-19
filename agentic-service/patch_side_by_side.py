import json

path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    if plan['designCode'].startswith('HP-4B'):
        layout = plan['layout']
        for r in layout['rooms']:
            if 'bath' in r['room_id']:
                # bath was x=11, w=8. Make it x=11, w=4.
                r['width'] = 4.0
                r['length'] = 12.0
                r['y'] = 10.0
            if 'hall' in r['room_id']:
                # hall was x=11, w=8. Make it x=15, w=4.
                r['x'] = 15.0
                r['width'] = 4.0
                r['length'] = 12.0
                r['y'] = 10.0

with open(path, 'w') as f:
    json.dump(data, f, indent=2)
print("Patched baths and halls side-by-side")
