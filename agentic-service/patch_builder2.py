with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'r') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    if 'stair_y = ' in line:
        new_lines.append('    stair_y = 17.0 if base_r == r2 else 20.0\n')
    elif 'rooms.append({"room_id": "stair_1"' in line:
        new_lines.append('    rooms.append({"room_id": "stair_1", "room_type": "staircase", "floor": 1, "x": 11, "y": stair_y, "width": 8, "length": 10})\n')
    elif 'rooms.append({"room_id": f"stair_{f}"' in line:
        new_lines.append('        rooms.append({"room_id": f"stair_{f}", "room_type": "staircase", "floor": f, "x": 11, "y": stair_y, "width": 8, "length": 10})\n')
    else:
        new_lines.append(line)

with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'w') as f:
    f.writelines(new_lines)
print("Patched indentation!")
