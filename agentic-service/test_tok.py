from app.design.models import Requirements
from app.design.plot_constraints import PlotConstraints
from app.design.base_plan_library import filter_compatible_base_plans
from pydantic import ValidationError

req = Requirements(bedrooms=3, floors=2)
plot = PlotConstraints(land_size_perches=10.0, plot_width_ft=50, plot_length_ft=50, edge_setbacks={'north': 10, 'south': 20, 'east': 10, 'west': 10})

compatible = filter_compatible_base_plans(req, plot)
print("Compatible base plans:", len(compatible))
for p in compatible:
    print(p.template_id)
