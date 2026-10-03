import pytest
from app.validation.design_validation_service import validate_house_plan
from app.schemas.validation_schemas import HousePlanValidationInput
from app.schemas.workflow_state import WorkflowState, CoordinatorInput

def test_runtime_no_legacy_fields():
    # Construct state without legacy fields (no preferences, no plot_constraints)
    import uuid
    input_data = CoordinatorInput(
        bedrooms=3,
        bathrooms=2,
        house_type="modern",
        land_size_perches=25,
        land_size_category="medium",
        submission_id=uuid.uuid4()
    )
    
    # State dict mock similar to what validate_house_plan processes
    # The design is valid
    state_dict = {
        "input_data": input_data.model_dump(),
        "terrain_result": {"terrain_type": "flat", "slope_estimate": "flat"},
        "design_result": {
            "ground_coverage_sqft": 2000,
            "bedrooms": 3,
            "bathrooms": 2,
            "floors": 1,
            "foundation_type": "slab",
            "rooms": [
                {"room_id": "r1", "room_type": "living", "floor": 1, "x": 0, "y": 0, "width": 10, "length": 10, "area_sqft": 100},
                {"room_id": "r2", "room_type": "bathroom", "floor": 1, "x": 10, "y": 0, "width": 5, "length": 5, "area_sqft": 25},
                {"room_id": "r3", "room_type": "bathroom", "floor": 1, "x": 15, "y": 0, "width": 5, "length": 5, "area_sqft": 25},
                {"room_id": "r4", "room_type": "kitchen", "floor": 1, "x": 0, "y": 10, "width": 10, "length": 10, "area_sqft": 100},
                {"room_id": "r5", "room_type": "bedroom", "floor": 1, "x": 10, "y": 10, "width": 10, "length": 10, "area_sqft": 100},
                {"room_id": "r6", "room_type": "bedroom", "floor": 1, "x": 20, "y": 10, "width": 10, "length": 10, "area_sqft": 100},
                {"room_id": "r7", "room_type": "bedroom", "floor": 1, "x": 30, "y": 10, "width": 10, "length": 10, "area_sqft": 100}
            ]
        },
        "cost_result": {
            "total_cost_lkr": 10000000
        },
        "budget_lkr": 12000000
    }
    
    # Act
    # Pass state_dict to validate_house_plan as if it were the raw object or dict
    result = validate_house_plan(state_dict)
    
    # Assert
    assert result.passed is True, "Validation should pass"
    
    # Verify preferences were correctly extracted from input_data, not a legacy dict
    pref_rule = next(r for r in result.rules if r.rule_name == "preferences")
    assert pref_rule.passed is True, "Preferences rule should pass by comparing 3 beds against 3 beds"
