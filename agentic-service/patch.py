import json

with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'r') as f:
    code = f.read()

# Fix validate_plan signature and plot
code = code.replace("def validate_plan(plan_data):", "def validate_plan(plan_data, terrain):")
code = code.replace("plot = PlotConstraints(land_size_perches=20.0, plot_width_ft=44, plot_length_ft=123)", "plot = PlotConstraints(land_size_perches=20.0, plot_width_ft=44, plot_length_ft=123, terrain_type=terrain)")
code = code.replace("is_valid, reason = validate_plan(plan)", "is_valid, reason = validate_plan(plan, terrain)")

# Fix make_rooms to just rename excess bedrooms to study instead of deleting
make_rooms_old = """    # Delete excess bedrooms completely
    beds_to_remove = current_beds - beds
    final_rooms = []
    for r in reversed(rooms):
        if beds_to_remove > 0 and 'bed' in r['room_id'] and r['room_id'] != 'bed_1':
            beds_to_remove -= 1
            continue
        final_rooms.append(r)
    rooms = list(reversed(final_rooms))"""

make_rooms_new = """    # Convert excess bedrooms to studies to avoid voids
    beds_to_remove = current_beds - beds
    for r in reversed(rooms):
        if beds_to_remove > 0 and 'bed' in r['room_id'] and r['room_id'] != 'bed_1':
            r['room_id'] = r['room_id'].replace('bed', 'study')
            r['room_type'] = 'study'
            beds_to_remove -= 1"""

code = code.replace(make_rooms_old, make_rooms_new)

with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'w') as f:
    f.write(code)
