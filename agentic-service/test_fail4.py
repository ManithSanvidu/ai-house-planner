from app.design.base_plan_library import load_base_plan_catalog
import json
catalog = load_base_plan_catalog()
for plan in catalog:
    if plan.plan_code == 'HP-4B1B-2F-559':
        design = json.loads(plan.layout_json)
        for r in design['rooms']:
            if 'bath' in r['room_type']:
                print(r['room_id'], r['floor'])
        break
