with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'r') as f:
    code = f.read()

# For r1
code = code.replace('"room_id": "bed_2", "room_type": "bedroom_2", "floor": 1, "x": 19, "y": 13, "width": 11', '"room_id": "bed_2", "room_type": "bedroom_2", "floor": 1, "x": 19.5, "y": 13, "width": 11')
code = code.replace('"room_id": "bed_3", "room_type": "bedroom_3", "floor": 1, "x": 19, "y": 0, "width": 11', '"room_id": "bed_3", "room_type": "bedroom_3", "floor": 1, "x": 19.5, "y": 0, "width": 11')
code = code.replace('"room_id": "hall_1", "room_type": "hallway", "floor": 1, "x": 16, "y": 10, "width": 3, "length": 10', '"room_id": "hall_1", "room_type": "hallway", "floor": 1, "x": 16, "y": 10, "width": 3.5, "length": 10')

# For r2
code = code.replace('"room_id": "bed_2", "room_type": "bedroom_2", "floor": 1, "x": 19, "y": 10, "width": 11', '"room_id": "bed_2", "room_type": "bedroom_2", "floor": 1, "x": 19.5, "y": 10, "width": 11')
code = code.replace('"room_id": "bed_3", "room_type": "bedroom_3", "floor": 1, "x": 19, "y": 0, "width": 11, "length": 10', '"room_id": "bed_3", "room_type": "bedroom_3", "floor": 1, "x": 19.5, "y": 0, "width": 11, "length": 10')
code = code.replace('"room_id": "hall_1", "room_type": "hallway", "floor": 1, "x": 16, "y": 8, "width": 3, "length": 9', '"room_id": "hall_1", "room_type": "hallway", "floor": 1, "x": 16, "y": 8, "width": 3.5, "length": 10')

with open('/Users/kushancs/.gemini/antigravity-ide/brain/3f960105-477a-4eff-a9cd-369acbee2f34/scratch/build_perfect_library_all.py', 'w') as f:
    f.write(code)
print("Patched widths!")
