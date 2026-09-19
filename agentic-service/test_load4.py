from app.design.base_plan_library import _load_seed_records, SEED_PATH, _record_from_design, DesignResult, validate_architectural_quality, _requirements_for, _base_plot
import json
seed_data = json.loads(SEED_PATH.read_text())
for source in seed_data:
    if source['bedrooms'] == 3 and source['bathrooms'] == 1 and source['floors'] == 2 and source['supportedTerrains'][0] == 'flat':
        design = DesignResult.model_validate(source['layout'])
        living = next(r for r in design.rooms if r.room_id == 'living')
        if living.length == 10:
            req = _requirements_for(design)
            plot = _base_plot(design, design.terrain_type)
            quality = validate_architectural_quality(design, req=req, plot=plot)
            print("Quality:", quality.passed, quality.status)
            print("Failures:", quality.failures)
            break
