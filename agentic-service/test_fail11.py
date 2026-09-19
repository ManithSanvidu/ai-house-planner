import json
import math
from app.schemas.design_result import DesignResult
from app.design.architectural_quality import validate_architectural_quality
from app.design.base_plan_library import _requirements_for, _base_plot

def centre(room): return room.x + room.width / 2, room.y + room.length / 2
dist = lambda a, b: math.hypot(centre(a)[0] - centre(b)[0], centre(a)[1] - centre(b)[1])

path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    if plan['designCode'] == 'HP-4B1B-2F-559':
        layout = plan['layout']
        design = DesignResult.model_validate(layout)
        req = _requirements_for(design)
        plot = _base_plot(design, "flat")
        quality = validate_architectural_quality(design, req, plot)
        print("Failures:", quality.failures)
        break
