import json

path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    if plan['designCode'].startswith('HP-4B'):
        layout = plan['layout']
        for r in layout['rooms']:
            if 'bath' in r['room_id'] and r['length'] == 4.0:
                r['length'] = 6.0
            if 'bath' in r['room_id'] and r['length'] == 3.6:
                r['length'] = 6.0
                
            if 'stair' in r['room_id'] and r['length'] == 7.5:
                # wait, previously I patched stair to 7.5 and y=20
                r['y'] = 22.0
                r['length'] = 7.5
                
            if r['room_id'] in ('bed_1', 'bed_2', 'study_4', 'study_5', 'bed_3', 'bed_4', 'bed_5'):
                # previously patched to 14.5
                if r['y'] == 13 and r['length'] in (14.5, 10.4):
                    r['length'] = 16.5
        
        # We need to fix adjacency/passage errors too!
        # "Family Room requires passage through an unrelated private room."
        # This happens if a public room ONLY connects to a bedroom!
        # Let's fix that by ensuring there is a hallway connection!
        # Wait, the base plan connections are dynamically computed!
        # I don't need to patch connections, they are computed by plan_adapter!

with open(path, 'w') as f:
    json.dump(data, f, indent=2)
print("Patched baths and stairs")
