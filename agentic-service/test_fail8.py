from app.tools.layout_generation_tool import prepare_inputs
from app.design.base_plan_library import load_base_plan_catalog
from app.design.plan_adapter import PlanAdapter
from app.schemas.ai_plan_decision import AIPlanDecision
from app.tools.geometry_validator import validate_geometry
import json
req, plot = prepare_inputs(
    land_size_perches=20.0,
    terrain_type="flat",
    preferences={"bedrooms": 4, "floors": 2}
)
catalog = load_base_plan_catalog()
found = False
for plan in catalog:
    if plan.plan_code.startswith('HP-4B'):
        found = True
        decision = AIPlanDecision.model_validate({
            'selected_plan_code': plan.plan_code,
            'alternative_plan_codes': [],
            'design_intent': {
                'public_zone_orientation': 'south',
                'private_zone_orientation': 'north',
                'service_zone_orientation': 'west',
                'privacy_priority': 'balanced',
                'circulation_preference': 'short_central_hall'
            },
            'adaptations': {
                'mirror_horizontal': False, 'mirror_vertical': False, 'rotation_degrees': 0,
                'living_scale': 1.0, 'bedroom_scale': 1.0, 'entrance_side': 'south',
                'preserve_stair_core': True, 'preserve_wet_core': True
            },
            'reason_codes': []
        })
        adapter = PlanAdapter()
        design = adapter.adapt(plan, decision, req, plot)
        geometry = validate_geometry(design.rooms, 4, 2, 20.0, plot=plot, design=design)
        if not geometry.passed:
            print(plan.plan_code, "Geometry failed:")
            for fail in geometry.failures:
                print("  ", fail)
            break
        else:
            print(plan.plan_code, "Geometry passed!")
            break
if not found:
    print("No 4B plans found!")
