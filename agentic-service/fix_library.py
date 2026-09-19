from app.design.base_plan_library import _record_from_design, SEED_PATH
from app.schemas.design_result import DesignResult, RoomLayout
from app.design.adjacency import build_connections
import json

r3 = [
    {"room_id": "living", "room_type": "living_room", "floor": 1, "x": 0, "y": 0, "width": 12, "length": 15},
    {"room_id": "kitchen", "room_type": "kitchen", "floor": 1, "x": 0, "y": 15, "width": 12, "length": 15},
    {"room_id": "bed_1", "room_type": "bedroom_1", "floor": 1, "x": 18, "y": 0, "width": 12, "length": 12},
    {"room_id": "bath_1", "room_type": "bathroom_1", "floor": 1, "x": 18, "y": 12, "width": 12, "length": 6},
    {"room_id": "stair_1", "room_type": "staircase", "floor": 1, "x": 18, "y": 18, "width": 12, "length": 12},
    {"room_id": "hall_1", "room_type": "hallway", "floor": 1, "x": 12, "y": 10, "width": 6, "length": 10}
]

def make_4b2f():
    rooms = []
    for r in r3:
        rooms.append(dict(r))
    
    for r in r3:
        new_r = dict(r)
        new_r['floor'] = 2
        if r['room_id'] == 'living':
            new_r['room_id'] = 'bed_2'
            new_r['room_type'] = 'bedroom_2'
        elif r['room_id'] == 'kitchen':
            new_r['room_id'] = 'bed_3'
            new_r['room_type'] = 'bedroom_3'
        elif r['room_id'] == 'bed_1':
            new_r['room_id'] = 'bed_4'
            new_r['room_type'] = 'bedroom_4'
        elif r['room_id'] == 'bath_1':
            new_r['room_id'] = 'bath_2'
            new_r['room_type'] = 'bathroom_2'
        elif r['room_id'] == 'stair_1':
            new_r['room_id'] = 'stair_2'
        elif r['room_id'] == 'hall_1':
            new_r['room_id'] = 'hall_2'
        rooms.append(new_r)
    return rooms

rooms = make_4b2f()
r_objs = [RoomLayout(**r) for r in rooms]
conns = build_connections(r_objs, False)
layout = {
    "template_family": "COMPACT_RECTANGLE",
    "template_id": "HP-4B2B-2F-PERFECT",
    "rooms": rooms,
    "connections": [c.model_dump() for c in conns],
    "entrances": [{"room_id": "living", "wall": "south", "offset": 3, "width": 4}],
    "floor_count": 2,
    "foundation_type": "slab",
    "total_built_up_area_sqft": sum(r['width'] * r['length'] for r in rooms),
    "ground_footprint_sqft": sum(r['width'] * r['length'] for r in rooms if r['floor'] == 1)
}
source = {
    "designCode": "HP-4B2B-2F-PERFECT",
    "name": "Perfect 4B2B 2F Plan",
    "minimumLandSizePerches": 10.0,
    "minimumPlotWidthFt": 40.0,
    "minimumPlotLengthFt": 40.0,
    "isActive": True,
    "layout": layout,
    "supportedTerrains": ["flat"]
}

design = DesignResult.model_validate(layout)
record = _record_from_design(source, design)
if not record:
    from app.design.architectural_quality import validate_architectural_quality
    from app.design.base_plan_library import _requirements_for, _base_plot
    from app.tools.geometry_validator import validate_geometry
    req = _requirements_for(design)
    plot = _base_plot(design, "flat")
    quality = validate_architectural_quality(design, req, plot)
    print("Quality failed:", quality.failures)
    geo = validate_geometry(design.rooms, 4, 2, 20.0, plot=plot, design=design)
    print("Geometry failed:", geo.failures)
else:
    print("PERFECT!")
    seed_data = json.loads(SEED_PATH.read_text())
    # Remove existing 4B plans so they don't get selected
    seed_data = [p for p in seed_data if not p.get('designCode', '').startswith('HP-4B')]
    seed_data.append(source)
    with open(SEED_PATH, 'w') as f:
        json.dump(seed_data, f, indent=2)
