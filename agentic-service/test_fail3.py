from app.tools.layout_generation_tool import prepare_inputs
from app.design.base_plan_library import filter_compatible_base_plans
import json
req, plot = prepare_inputs(
    land_size_perches=20.0,
    terrain_type="flat",
    preferences={"bedrooms": 4, "floors": 2}
)
compatible = filter_compatible_base_plans(req, plot)
print("Compatible base plans:", len(compatible))
for plan in compatible:
    print(plan.plan_code)
