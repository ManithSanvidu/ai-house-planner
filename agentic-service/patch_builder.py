import re
with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'r') as f:
    code = f.read()

r1_new = """r1 = [
    {"room_id": "living", "room_type": "living_room", "floor": 1, "x": 0, "y": 0, "width": 11, "length": 13},
    {"room_id": "bed_1", "room_type": "bedroom_1", "floor": 1, "x": 0, "y": 13, "width": 11, "length": 17},
    {"room_id": "bed_2", "room_type": "bedroom_2", "floor": 1, "x": 19, "y": 13, "width": 11, "length": 17},
    {"room_id": "bed_3", "room_type": "bedroom_3", "floor": 1, "x": 19, "y": 0, "width": 11, "length": 13},
    {"room_id": "kitchen", "room_type": "kitchen", "floor": 1, "x": 11, "y": 0, "width": 8, "length": 10},
    {"room_id": "hall_1", "room_type": "hallway", "floor": 1, "x": 16, "y": 10, "width": 3, "length": 10},
    {"room_id": "bath_1", "room_type": "bathroom_1", "floor": 1, "x": 11, "y": 10, "width": 5, "length": 10}
]"""

r2_new = """r2 = [
    {"room_id": "living", "room_type": "living_room", "floor": 1, "x": 0, "y": 0, "width": 11, "length": 10},
    {"room_id": "bed_1", "room_type": "bedroom_1", "floor": 1, "x": 0, "y": 10, "width": 11, "length": 17},
    {"room_id": "bed_2", "room_type": "bedroom_2", "floor": 1, "x": 19, "y": 10, "width": 11, "length": 17},
    {"room_id": "bed_3", "room_type": "bedroom_3", "floor": 1, "x": 19, "y": 0, "width": 11, "length": 10},
    {"room_id": "kitchen", "room_type": "kitchen", "floor": 1, "x": 11, "y": 0, "width": 8, "length": 8},
    {"room_id": "hall_1", "room_type": "hallway", "floor": 1, "x": 16, "y": 8, "width": 3, "length": 9},
    {"room_id": "bath_1", "room_type": "bathroom_1", "floor": 1, "x": 11, "y": 8, "width": 5, "length": 9}
]"""

code = re.sub(r'r1 = \[.*?\]\n', r1_new + '\n', code, flags=re.DOTALL)
code = re.sub(r'r2 = \[.*?\]\n', r2_new + '\n', code, flags=re.DOTALL)

# REMOVE the resize logic for bath_1 in floors>1 because it's no longer needed
code = re.sub(r'    if floors > 1:\n        for r in rooms:\n            if r\[\'room_id\'\] == \'bath_1\':\n                if base_r == r2:\n                    r\[\'length\'\] = 3.6\n                else:\n                    r\[\'length\'\] = 4.0\n', '', code)

# Update the staircase injection logic
# stair_y = 20 for r1, 17 for r2
code = re.sub(r'stair_y = 16.6 if base_r == r2 else 20.0', 'stair_y = 17 if base_r == r2 else 20.0', code)
# width: 8, length: 10
code = re.sub(r'"width": 8, "length": 3.4', '"width": 8, "length": 10', code)

with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'w') as f:
    f.write(code)
print("Patched builder!")
