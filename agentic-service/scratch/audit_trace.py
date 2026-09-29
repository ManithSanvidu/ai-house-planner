import os, sys
# add app to path
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))
from app.design.catalogue.base_plan_library import load_base_plan_catalog
from app.design.generation.generation_service import filter_compatible_base_plans, _candidate_pool, rank_base_plans, deduplicate_base_plans
from app.design.program.models import Requirements
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.catalogue.plan_suitability import suitability_breakdown

plans = load_base_plan_catalog()
print("Loaded Plan Codes:", [p.plan_code for p in plans])

req = Requirements(bedrooms=3, bathrooms=2, floors=1, style="Tropical")
plot = PlotConstraints(plot_width_ft=60, plot_length_ft=80, land_size_perches=15, road_side="south", north_direction="north", entrance_side="south")

compatible = filter_compatible_base_plans(req, plot)
print("Compatible:", [p.plan_code for p in compatible])

ranked = rank_base_plans(compatible, req, plot)
print("Ranked:", [(p.plan_code, suitability_breakdown(p, req, plot)['score']) for p in ranked])

if ranked:
    top = ranked[0]
    print(f"Top Plan: {top.plan_code}")
    print("Exact Score:", suitability_breakdown(top, req, plot))

