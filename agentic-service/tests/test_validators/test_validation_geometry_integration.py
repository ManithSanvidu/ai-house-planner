from app.validation.design_validation_service import validate_house_plan
from app.schemas.validation_schemas import HousePlanValidationInput
from app.schemas.design_result import RoomLayout

def test_geometry_integration_pass():
    # A. valid geometry
    rooms = [
        RoomLayout(room_id="r1", room_type="living", floor=1, x=0, y=0, width=10, length=10, area_sqft=100),
        RoomLayout(room_id="r2", room_type="kitchen", floor=1, x=10, y=0, width=10, length=10, area_sqft=100),
        RoomLayout(room_id="r3", room_type="bath", floor=1, x=0, y=10, width=5, length=5, area_sqft=25),
        RoomLayout(room_id="r4", room_type="bedroom", floor=1, x=10, y=10, width=10, length=10, area_sqft=100),
    ]
    val_input = HousePlanValidationInput(
        land_size_perches=10,
        ground_coverage_sqft=325,
        terrain_type="flat",
        foundation_type="strip",
        budget_lkr=None,
        estimated_cost_lkr=None,
        requested_bedrooms=1,
        actual_bedrooms=1,
        requested_bathrooms=1,
        actual_bathrooms=1,
        requested_floors=1,
        actual_floors=1,
        rooms=rooms
    )
    result = validate_house_plan(val_input)
    assert result.passed is True
    geom_rule = next((r for r in result.rules if r.rule_name == "geometry"), None)
    assert geom_rule is not None
    assert geom_rule.passed is True
    assert "Geometry validation passed" in geom_rule.reason

def test_geometry_integration_overlap_fail():
    # B. overlapping rooms
    rooms = [
        RoomLayout(room_id="r1", room_type="living", floor=1, x=0, y=0, width=10, length=10, area_sqft=100),
        RoomLayout(room_id="r2", room_type="kitchen", floor=1, x=5, y=5, width=10, length=10, area_sqft=100), # overlaps r1
        RoomLayout(room_id="r3", room_type="bath", floor=1, x=0, y=20, width=5, length=5, area_sqft=25),
        RoomLayout(room_id="r4", room_type="bedroom", floor=1, x=10, y=20, width=10, length=10, area_sqft=100),
    ]
    val_input = HousePlanValidationInput(
        land_size_perches=10,
        ground_coverage_sqft=325,
        terrain_type="flat",
        foundation_type="strip",
        rooms=rooms
    )
    result = validate_house_plan(val_input)
    assert result.passed is False
    geom_rule = next((r for r in result.rules if r.rule_name == "geometry"), None)
    assert geom_rule is not None
    assert geom_rule.passed is False
    assert "overlap" in geom_rule.reason.lower()

def test_geometry_integration_out_of_bounds_fail():
    # C. room outside building bounds (too much area for small land)
    rooms = [
        RoomLayout(room_id="r1", room_type="living", floor=1, x=0, y=0, width=10, length=10, area_sqft=100),
        RoomLayout(room_id="r2", room_type="kitchen", floor=1, x=10, y=0, width=10, length=10, area_sqft=100),
        RoomLayout(room_id="r3", room_type="bath", floor=1, x=0, y=10, width=5, length=5, area_sqft=25),
        RoomLayout(room_id="r4", room_type="bedroom", floor=1, x=10, y=10, width=50, length=50, area_sqft=2500), # exceeds max perches
    ]
    val_input = HousePlanValidationInput(
        land_size_perches=2, # Very small land
        ground_coverage_sqft=2725,
        terrain_type="flat",
        foundation_type="strip",
        rooms=rooms
    )
    result = validate_house_plan(val_input)
    assert result.passed is False
    geom_rule = next((r for r in result.rules if r.rule_name == "geometry"), None)
    assert geom_rule is not None
    assert geom_rule.passed is False
    assert "exceeds" in geom_rule.reason.lower() or "coverage" in geom_rule.reason.lower() or "bounds" in geom_rule.reason.lower()

def test_geometry_integration_invalid_dimensions():
    # D. invalid/negative dimensions
    rooms = [
        {"room_id": "r1", "room_type": "living", "floor": 1, "x": 0, "y": 0, "width": -10, "length": 10, "area_sqft": -100}
    ]
    val_input = HousePlanValidationInput(
        land_size_perches=10,
        ground_coverage_sqft=100,
        terrain_type="flat",
        foundation_type="strip",
        rooms=rooms
    )
    result = validate_house_plan(val_input)
    assert result.passed is False
    geom_rule = next((r for r in result.rules if r.rule_name == "geometry"), None)
    assert geom_rule is not None
    assert geom_rule.passed is False
    assert "error" in geom_rule.reason.lower() or "invalid" in geom_rule.reason.lower()
