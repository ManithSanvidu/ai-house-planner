from app.design.base_plan_library import _load_seed_records, SEED_PATH, _record_from_design, DesignResult, validate_architectural_quality, _requirements_for, _base_plot
import json
seed_data = json.loads(SEED_PATH.read_text())
source = seed_data[0]
design = DesignResult.model_validate(source['layout'])
req = _requirements_for(design)
plot = _base_plot(design, design.terrain_type)
for r in design.rooms:
    print(r.room_id, r.x, r.y, r.width, r.length)
print("total bounding box test:", max(r.x + r.width for r in design.rooms), max(r.y + r.length for r in design.rooms))
