import json
from app.design.architectural_quality import validate_architectural_quality
from app.schemas.design_result import DesignResult
from app.design.base_plan_library import _requirements_for, _base_plot

path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    if plan['designCode'].startswith('HP-4B'):
        layout = plan['layout']
        design = DesignResult.model_validate(layout)
        req = _requirements_for(design)
        plot = _base_plot(design, "flat")
        quality = validate_architectural_quality(design, req, plot)
        if not quality.passed:
            print(plan['designCode'], "Quality failed:", quality.failures)
        else:
            print(plan['designCode'], "Quality passed!")
        break
