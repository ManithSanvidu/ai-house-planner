from app.validation.design_validation_service import validate_house_plan
from app.schemas.validation_schemas import HousePlanValidationInput


def _valid_input(**overrides):
    values = {
        "land_size_perches": 10,
        "ground_coverage_sqft": 1000,
        "terrain_type": "flat",
        "foundation_type": "strip",
        "budget_lkr": 10_000_000,
        "estimated_cost_lkr": 9_000_000,
        "requested_bedrooms": 2,
        "actual_bedrooms": 2,
        "requested_bathrooms": 1,
        "actual_bathrooms": 1,
        "requested_floors": 1,
        "actual_floors": 1,
    }
    values.update(overrides)
    return HousePlanValidationInput(**values)


def test_final_validation_contains_only_supported_rule_groups():
    result = validate_house_plan(_valid_input())

    assert [rule.rule_name for rule in result.rules] == [
        "coverage",
        "terrain_foundation",
        "budget",
        "preferences",
    ]
    assert result.passed is True


def test_invalid_geometry_is_not_a_final_validation_rule_or_failure():
    result = validate_house_plan(
        _valid_input(
            rooms=[
                {"room_id": "bad", "room_type": "living", "floor": 1,
                 "x": 0, "y": 0, "width": -10, "length": 10, "area_sqft": -100}
            ]
        )
    )

    assert all(rule.rule_name != "geometry" for rule in result.rules)
    assert result.passed is True


def test_remaining_rule_failure_fails_overall_validation():
    result = validate_house_plan(_valid_input(ground_coverage_sqft=2000))

    assert result.passed is False
    assert next(rule for rule in result.rules if rule.rule_name == "coverage").status == "FAIL"


def test_not_applicable_budget_does_not_fail_overall_validation():
    result = validate_house_plan(_valid_input(budget_lkr=None, estimated_cost_lkr=None))

    budget = next(rule for rule in result.rules if rule.rule_name == "budget")
    assert budget.status == "NOT_APPLICABLE"
    assert result.passed is True
