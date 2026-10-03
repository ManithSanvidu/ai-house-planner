from app.validation.design_validation_service import validate_budget

def test_validate_budget_not_applicable():
    result = validate_budget(budget_lkr=None, estimated_cost_lkr=None)
    assert result.passed is True
    assert result.status == "NOT_APPLICABLE"
    assert "not applicable" in result.reason.lower()

def test_validate_budget_pass():
    result = validate_budget(budget_lkr=1000000, estimated_cost_lkr=1050000)
    assert result.passed is True
    assert result.status == "PASS"

def test_validate_budget_fail():
    result = validate_budget(budget_lkr=1000000, estimated_cost_lkr=1200000)
    assert result.passed is False
    assert result.status == "FAIL"
