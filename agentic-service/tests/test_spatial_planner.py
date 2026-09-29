from app.design.program.models import Requirements
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.generation.spatial_planner import plan_spatial_program

def mock_plot_constraints():
    return PlotConstraints.model_validate({
        'land_size_perches': 20,
        'plot_width_ft': 60,
        'plot_length_ft': 80,
        'road_side': 'south',
        'terrain_type': 'flat',
        'setbacks': {'front': 10, 'rear': 10, 'left': 5, 'right': 5},
    })

def test_deterministic_spatial_planning():
    req = Requirements(bedrooms=3, bathrooms=2, floors=1)
    plot = mock_plot_constraints()
    
    program, meta = plan_spatial_program(req, plot)
    
    assert program.floor_count == 1
    assert len([r for r in program.rooms if "bedroom" in r.type]) == 3
    assert len([r for r in program.rooms if "bathroom" in r.type]) == 2
    assert meta["total_target_area"] == sum(r.target_area_sqft for r in program.rooms)

