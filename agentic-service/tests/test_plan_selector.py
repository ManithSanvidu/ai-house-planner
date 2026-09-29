import pytest
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.program.models import Requirements
from app.design.catalogue.plan_selector import PlanSelector
import app.design.catalogue.plan_selector as ps

def test_plan_selector_no_match(monkeypatch):
    monkeypatch.setattr(ps, "load_base_plan_catalog", lambda: [])
    monkeypatch.setattr(ps, "filter_compatible_base_plans", lambda req, plot: [])
    
    req = Requirements(bedrooms=8, bathrooms=6, floors=1, min_area_sqm=100.0, max_area_sqm=200.0, features=[])
    plot = PlotConstraints(plot_width=10, plot_length=10, land_size_perches=10, road_frontage_side="SOUTH", road_width=5)
    
    selected = PlanSelector.select_plan(req, plot)
    assert selected is None

def test_plan_selector_match(monkeypatch):
    class MockRecord:
        def __init__(self, code):
            self.base_plan_code = code

    mock_plan1 = MockRecord("PLAN_1")
    mock_plan2 = MockRecord("PLAN_2")
    
    monkeypatch.setattr(ps, "load_base_plan_catalog", lambda: [mock_plan1, mock_plan2])
    monkeypatch.setattr(ps, "filter_compatible_base_plans", lambda req, plot: [mock_plan1, mock_plan2])
    monkeypatch.setattr(ps, "rank_base_plans", lambda candidates, req, plot: [mock_plan1, mock_plan2])
    
    req = Requirements(bedrooms=3, bathrooms=2, floors=1, min_area_sqm=100.0, max_area_sqm=200.0, features=[])
    plot = PlotConstraints(plot_width=10, plot_length=10, land_size_perches=10, road_frontage_side="SOUTH", road_width=5)
    
    selected = PlanSelector.select_plan(req, plot)
    assert selected is not None
    assert selected.base_plan_code == "PLAN_1"
