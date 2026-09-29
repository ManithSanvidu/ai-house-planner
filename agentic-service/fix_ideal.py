import json

def check_circ(rooms):
    internal = sum(r['width']*r['length'] for r in rooms)
    circ = sum(r['width']*r['length'] for r in rooms if r['room_type'] in ['corridor', 'hallway', 'foyer', 'entrance', 'staircase'])
    return circ / internal if internal else 1

templates = []

# TPL-S-1F-2B1B
t1_rooms = [
    {"room_id": "liv", "name": "Living Room", "room_type": "living_room", "floor": 1, "x": 0, "y": 0, "width": 14, "length": 14},
    {"room_id": "kit", "name": "Kitchen", "room_type": "kitchen", "floor": 1, "x": 0, "y": 14, "width": 14, "length": 10},
    {"room_id": "din", "name": "Dining", "room_type": "dining", "floor": 1, "x": 0, "y": 24, "width": 14, "length": 8},
    {"room_id": "hall_1", "name": "Main Corridor", "room_type": "hallway", "floor": 1, "x": 14, "y": 0, "width": 4, "length": 18},
    {"room_id": "bedroom_1", "name": "Master Bed", "room_type": "bedroom", "floor": 1, "x": 18, "y": 0, "width": 14, "length": 12},
    {"room_id": "bath1", "name": "Bath", "room_type": "bathroom", "floor": 1, "x": 18, "y": 12, "width": 14, "length": 6},
    {"room_id": "bed2", "name": "Guest Bed", "room_type": "bedroom", "floor": 1, "x": 14, "y": 18, "width": 18, "length": 14}
]
templates.append({
    "template_id": "TPL-S-1F-2B1B", "land_category": "small", "minimum_perches": 5, "maximum_perches": 15, "floors": 1,
    "entrances": [{"room_id": "liv"}],
    "connections": [
        {"from_room": "liv", "to_room": "hall_1"},
        {"from_room": "kit", "to_room": "hall_1"},
        {"from_room": "liv", "to_room": "kit"},
        {"from_room": "kit", "to_room": "din"},
        {"from_room": "bedroom_1", "to_room": "hall_1"},
        {"from_room": "bath1", "to_room": "hall_1"},
        {"from_room": "bed2", "to_room": "hall_1"}
    ],
    "rooms": t1_rooms
})

# TPL-S-1F-3B2B
t2_rooms = [
    {"room_id": "liv", "name": "Living Room", "room_type": "living_room", "floor": 1, "x": 12, "y": 0, "width": 12, "length": 16},
    {"room_id": "bedroom_1", "name": "Master Bed", "room_type": "bedroom", "floor": 1, "x": 0, "y": 0, "width": 12, "length": 10},
    {"room_id": "bath1", "name": "Ensuite", "room_type": "bathroom_attached", "floor": 1, "x": 0, "y": 10, "width": 12, "length": 6},
    {"room_id": "din", "name": "Dining", "room_type": "dining", "floor": 1, "x": 12, "y": 16, "width": 12, "length": 16},
    {"room_id": "kit", "name": "Kitchen", "room_type": "kitchen", "floor": 1, "x": 0, "y": 16, "width": 12, "length": 16},
    {"room_id": "bed2", "name": "Bed 2", "room_type": "bedroom", "floor": 1, "x": 24, "y": 0, "width": 12, "length": 16},
    {"room_id": "bed3", "name": "Bed 3", "room_type": "bedroom", "floor": 1, "x": 24, "y": 16, "width": 12, "length": 10},
    {"room_id": "bath2", "name": "Common Bath", "room_type": "bathroom", "floor": 1, "x": 24, "y": 26, "width": 12, "length": 6}
]
templates.append({
    "template_id": "TPL-S-1F-3B2B", "land_category": "small", "minimum_perches": 7, "maximum_perches": 15, "floors": 1,
    "entrances": [{"room_id": "liv"}],
    "connections": [
        {"from_room": "liv", "to_room": "din"},
        {"from_room": "din", "to_room": "kit"},
        {"from_room": "liv", "to_room": "bedroom_1"},
        {"from_room": "bedroom_1", "to_room": "bath1"},
        {"from_room": "liv", "to_room": "bed2"},
        {"from_room": "din", "to_room": "bed3"},
        {"from_room": "din", "to_room": "bath2"}
    ],
    "rooms": t2_rooms
})

# TPL-S-2F-3B2B
t3_rooms = [
    {"room_id": "liv", "name": "Living Room", "room_type": "living_room", "floor": 1, "x": 0, "y": 0, "width": 10, "length": 24},
    {"room_id": "kit", "name": "Kitchen", "room_type": "kitchen", "floor": 1, "x": 10, "y": 0, "width": 20, "length": 10},
    {"room_id": "hall_1", "name": "Hall", "room_type": "hallway", "floor": 1, "x": 10, "y": 10, "width": 8, "length": 4},
    {"room_id": "stair_1", "name": "Staircase", "room_type": "staircase", "floor": 1, "x": 10, "y": 14, "width": 8, "length": 4},
    {"room_id": "din", "name": "Dining", "room_type": "dining", "floor": 1, "x": 18, "y": 10, "width": 12, "length": 14},
    {"room_id": "bath1", "name": "Guest Bath", "room_type": "bathroom", "floor": 1, "x": 10, "y": 18, "width": 8, "length": 6},
    
    # F2:
    {"room_id": "bedroom_1", "name": "Master Bed", "room_type": "bedroom", "floor": 2, "x": 0, "y": 0, "width": 10, "length": 24},
    {"room_id": "bed2", "name": "Bed 2", "room_type": "bedroom", "floor": 2, "x": 10, "y": 0, "width": 20, "length": 10},
    {"room_id": "hall_2", "name": "Hall", "room_type": "hallway", "floor": 2, "x": 10, "y": 10, "width": 8, "length": 4},
    {"room_id": "stair_2", "name": "Landing", "room_type": "staircase", "floor": 2, "x": 10, "y": 14, "width": 8, "length": 4},
    {"room_id": "bed3", "name": "Bed 3", "room_type": "bedroom", "floor": 2, "x": 18, "y": 10, "width": 12, "length": 14},
    {"room_id": "bath2", "name": "Common Bath", "room_type": "bathroom", "floor": 2, "x": 10, "y": 18, "width": 8, "length": 6}
]
templates.append({
    "template_id": "TPL-S-2F-3B2B", "land_category": "small", "minimum_perches": 6, "maximum_perches": 15, "floors": 2,
    "entrances": [{"room_id": "liv"}],
    "connections": [
        {"from_room": "liv", "to_room": "hall_1"},
        {"from_room": "kit", "to_room": "hall_1"},
        {"from_room": "din", "to_room": "hall_1"},
        {"from_room": "hall_1", "to_room": "stair_1"},
        {"from_room": "liv", "to_room": "bath1"},
        {"from_room": "stair_1", "to_room": "stair_2"},
        {"from_room": "stair_2", "to_room": "hall_2"},
        {"from_room": "hall_2", "to_room": "bedroom_1"},
        {"from_room": "hall_2", "to_room": "bed2"},
        {"from_room": "hall_2", "to_room": "bed3"},
        {"from_room": "stair_2", "to_room": "bath2"}
    ],
    "rooms": t3_rooms
})

# TPL-M-1F-3B2B
t4_rooms = [
    {"room_id": "liv", "name": "Living Room", "room_type": "living_room", "floor": 1, "x": 14, "y": 0, "width": 14, "length": 16},
    {"room_id": "bedroom_1", "name": "Master Bed", "room_type": "bedroom", "floor": 1, "x": 0, "y": 0, "width": 14, "length": 10},
    {"room_id": "bath1", "name": "Ensuite", "room_type": "bathroom_attached", "floor": 1, "x": 0, "y": 10, "width": 14, "length": 6},
    {"room_id": "din", "name": "Dining", "room_type": "dining", "floor": 1, "x": 14, "y": 16, "width": 14, "length": 16},
    {"room_id": "kit", "name": "Kitchen", "room_type": "kitchen", "floor": 1, "x": 0, "y": 16, "width": 14, "length": 16},
    {"room_id": "bed2", "name": "Bed 2", "room_type": "bedroom", "floor": 1, "x": 28, "y": 0, "width": 14, "length": 16},
    {"room_id": "bed3", "name": "Bed 3", "room_type": "bedroom", "floor": 1, "x": 28, "y": 16, "width": 14, "length": 10},
    {"room_id": "bath2", "name": "Common Bath", "room_type": "bathroom", "floor": 1, "x": 28, "y": 26, "width": 14, "length": 6}
]
templates.append({
    "template_id": "TPL-M-1F-3B2B", "land_category": "medium", "minimum_perches": 16, "maximum_perches": 30, "floors": 1,
    "entrances": [{"room_id": "liv"}],
    "connections": [
        {"from_room": "liv", "to_room": "din"},
        {"from_room": "din", "to_room": "kit"},
        {"from_room": "liv", "to_room": "bedroom_1"},
        {"from_room": "bedroom_1", "to_room": "bath1"},
        {"from_room": "liv", "to_room": "bed2"},
        {"from_room": "din", "to_room": "bed3"},
        {"from_room": "din", "to_room": "bath2"}
    ],
    "rooms": t4_rooms
})

# TPL-M-2F-4B3B (32x28)
t5_rooms = [
    # F1
    {"room_id": "liv", "name": "Living Room", "room_type": "living_room", "floor": 1, "x": 0, "y": 0, "width": 16, "length": 18},
    {"room_id": "bed1", "name": "Guest Bed", "room_type": "bedroom", "floor": 1, "x": 16, "y": 0, "width": 16, "length": 10},
    {"room_id": "bath1", "name": "Guest Bath", "room_type": "bathroom", "floor": 1, "x": 16, "y": 10, "width": 10, "length": 8},
    {"room_id": "stair_1", "name": "Staircase", "room_type": "staircase", "floor": 1, "x": 26, "y": 10, "width": 6, "length": 8},
    {"room_id": "din", "name": "Dining", "room_type": "dining", "floor": 1, "x": 0, "y": 18, "width": 16, "length": 10},
    {"room_id": "kit", "name": "Kitchen", "room_type": "kitchen", "floor": 1, "x": 16, "y": 18, "width": 16, "length": 10},
    
    # F2
    {"room_id": "bedroom_1", "name": "Master Bed", "room_type": "bedroom", "floor": 2, "x": 0, "y": 0, "width": 12, "length": 14},
    {"room_id": "bed2", "name": "Bed 2", "room_type": "bedroom", "floor": 2, "x": 0, "y": 14, "width": 12, "length": 14},
    {"room_id": "bath2", "name": "Ensuite", "room_type": "bathroom_attached", "floor": 2, "x": 12, "y": 0, "width": 10, "length": 10},
    {"room_id": "bath3", "name": "Common Bath", "room_type": "bathroom", "floor": 2, "x": 12, "y": 18, "width": 10, "length": 10},
    {"room_id": "hall_2", "name": "Upper Corridor", "room_type": "hallway", "floor": 2, "x": 12, "y": 10, "width": 14, "length": 8},
    {"room_id": "stair_2", "name": "Landing", "room_type": "staircase", "floor": 2, "x": 26, "y": 10, "width": 6, "length": 8},
    {"room_id": "bed3", "name": "Bed 3", "room_type": "bedroom", "floor": 2, "x": 22, "y": 0, "width": 10, "length": 10},
    {"room_id": "bed4", "name": "Bed 4", "room_type": "bedroom", "floor": 2, "x": 22, "y": 18, "width": 10, "length": 10}
]
templates.append({
    "template_id": "TPL-M-2F-4B3B", "land_category": "medium", "minimum_perches": 16, "maximum_perches": 30, "floors": 2,
    "entrances": [{"room_id": "liv"}],
    "connections": [
        {"from_room": "liv", "to_room": "din"},
        {"from_room": "din", "to_room": "kit"},
        {"from_room": "liv", "to_room": "bed1"},
        {"from_room": "bed1", "to_room": "bath1"},
        {"from_room": "kit", "to_room": "stair_1"},
        {"from_room": "stair_1", "to_room": "stair_2"},
        {"from_room": "stair_2", "to_room": "hall_2"},
        {"from_room": "hall_2", "to_room": "bedroom_1"},
        {"from_room": "hall_2", "to_room": "bed2"},
        {"from_room": "hall_2", "to_room": "bed3"},
        {"from_room": "hall_2", "to_room": "bed4"},
        {"from_room": "bedroom_1", "to_room": "bath2"},
        {"from_room": "hall_2", "to_room": "bath3"}
    ],
    "rooms": t5_rooms
})

with open("approved_templates.json", "w") as f:
    json.dump(templates, f, indent=2)

print("Generated approved_templates.json")
