from app.tools.layout_generation_tool import prepare_inputs
from app.design.base_plan_library import filter_compatible_base_plans, load_base_plan_catalog
import json
req, plot = prepare_inputs(
    land_size_perches=20.0,
    terrain_type="flat",
    preferences={"bedrooms": 4, "floors": 2}
)

catalog = load_base_plan_catalog()
four_bed_plans = [p for p in catalog if p.bedrooms == 4 and p.floors == 2 and 'flat' in p.supported_terrains]
print("4B 2F flat plans in catalog:", len(four_bed_plans))

for plan in four_bed_plans:
    if plan.minimum_land_perches > plot.land_size_perches: print("Failed land size", plan.plan_code)
    elif plan.minimum_plot_width_ft and plan.minimum_plot_width_ft > plot.plot_width_ft: print("Failed plot width", plan.plan_code)
    elif plan.minimum_plot_length_ft and plan.minimum_plot_length_ft > plot.plot_length_ft: print("Failed plot length", plan.plan_code)
    else: print("Valid?", plan.plan_code)
