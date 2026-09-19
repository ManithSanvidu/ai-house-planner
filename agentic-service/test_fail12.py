from app.design.architectural_quality import validate_architectural_quality
from app.schemas.design_result import DesignResult
from app.design.base_plan_library import _requirements_for, _base_plot
import json

path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    if plan['designCode'] == 'HP-4B1B-2F-559':
        layout = plan['layout']
        design = DesignResult.model_validate(layout)
        req = _requirements_for(design)
        plot = _base_plot(design, "flat")
        # Let's re-implement bed_distance here
        import math
        def room_kind(t):
            if 'bed' in t or 'study' in t: return 'bedroom'
            return t
        beds = [room for room in design.rooms if room_kind(room.room_type) == 'bedroom']
        def centre(room): return room.x + room.width / 2, room.y + room.length / 2
        dist = lambda a, b: math.hypot(centre(a)[0] - centre(b)[0], centre(a)[1] - centre(b)[1])
        bed_distance = max((dist(a, b) for i, a in enumerate(beds) for b in beds[i + 1:] if a.floor == b.floor), default=0)
        print("bed_distance:", bed_distance)
        break
