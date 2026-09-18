import json
from app.design.architectural_quality import validate_architectural_quality
from app.schemas.design_result import DesignResult

path = '/Users/kushancs/Desktop/ai-house-system/HousePlanner.API/Data/Seed/pre-designed-plans.json'
with open(path, 'r') as f:
    data = json.load(f)

for plan in data:
    if plan['designCode'] == 'HP-4B1B-2F-559':
        print(plan['layout']['connections'])
        break
