from app.design.base_plan_library import load_base_plan_catalog, filter_compatible_base_plans
from app.design.models import Requirements
from app.design.plot_constraints import PlotConstraints
import sys

req = Requirements(bedrooms=3, bathrooms=1, floors=2, style='modern', open_plan=False, dining_required=False, master_bedroom=False, design_seed=42)
plot = PlotConstraints(land_size_perches=10.0, plot_width_ft=50.0, plot_length_ft=50.0, road_side='south', north_direction='top', terrain_type='flat', setbacks={'front': 10, 'rear': 7, 'left': 5, 'right': 5})
plans = filter_compatible_base_plans(req, plot)
print("Compatible plans:", len(plans))
for p in load_base_plan_catalog():
    if p.floors == 2 and p.bedrooms == 3 and p.bathrooms == 1 and p.supported_terrains == ['flat']:
        print("Checking plan:", p.plan_code)
        print("is_active:", p.is_active)
        print("land:", p.minimum_land_perches, plot.land_size_perches)
        print("width:", p.minimum_plot_width_ft, plot.buildable_width)
        print("length:", p.minimum_plot_length_ft, plot.buildable_length)
        print("shape:", plot.plot_class, p.supported_plot_shapes)
        break
