import json

path = 'app/design/seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    if plan['designCode'] == 'HP-4B1B-2F-559':
        print("Found 559!")
        layout = plan['layout']
        for r in layout['rooms']:
            if r['room_id'] == 'bath_2':
                # Resize bath_2 to length 3.6 to match bath_1 (4.0 in r1)
                r['length'] = 4.0
            if r['room_id'] == 'stair_1' or r['room_id'] == 'stair_2':
                # Staircase is 8x3.4. Needs to be at least 60 area (e.g. 8x7.5).
                # But it's constrained by y=20. If length becomes 7.5, it overlaps something?
                # It's at y=20.
                pass
