from app.design.adjacency import build_connections
from app.schemas.design_result import RoomLayout

r1 = [
    {"room_id": "living", "room_type": "living_room", "floor": 1, "x": 0, "y": 0, "width": 11, "length": 13},
    {"room_id": "bed_1", "room_type": "bedroom_1", "floor": 1, "x": 0, "y": 13, "width": 11, "length": 17},
    {"room_id": "bed_2", "room_type": "bedroom_2", "floor": 1, "x": 19, "y": 13, "width": 11, "length": 17},
    {"room_id": "bed_3", "room_type": "bedroom_3", "floor": 1, "x": 19, "y": 0, "width": 11, "length": 13},
    {"room_id": "kitchen", "room_type": "kitchen", "floor": 1, "x": 11, "y": 0, "width": 8, "length": 10},
    {"room_id": "hall_1", "room_type": "hallway", "floor": 1, "x": 16, "y": 10, "width": 3, "length": 10},
    {"room_id": "bath_1", "room_type": "bathroom_1", "floor": 1, "x": 11, "y": 10, "width": 5, "length": 10},
    {"room_id": "stair_1", "room_type": "staircase", "floor": 1, "x": 11, "y": 20, "width": 8, "length": 10}
]

r_objs = [RoomLayout(**r) for r in r1]
conns = build_connections(r_objs, False)
for c in conns:
    if 'stair' in c.from_room or 'stair' in c.to_room:
        print(c)
