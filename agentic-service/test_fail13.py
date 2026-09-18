import json
from app.design.architectural_quality import validate_architectural_quality, QUALITY
from app.schemas.design_result import DesignResult
from app.design.base_plan_library import _requirements_for, _base_plot
from unittest.mock import patch

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
        break
