from app.validation.design_validation_service import validate_preferences

def test_validate_preferences_bathroom_match():
    # A. bathroom match
    result = validate_preferences(
        requested_bedrooms=None,
        actual_bedrooms=None,
        requested_bathrooms=1,
        actual_bathrooms=1,
        requested_floors=None,
        actual_floors=None,
    )
    assert result.passed is True
    assert "Bathrooms: 1" in result.reason

def test_validate_preferences_bathroom_shortage():
    # B. bathroom shortage
    result = validate_preferences(
        requested_bedrooms=None,
        actual_bedrooms=None,
        requested_bathrooms=2,
        actual_bathrooms=1,
        requested_floors=None,
        actual_floors=None,
    )
    assert result.passed is False
    assert "Bathroom count mismatch: client requested 2 bathroom(s), but design provides 1" in result.reason

def test_validate_preferences_bathroom_excess():
    # C. bathroom excess
    result = validate_preferences(
        requested_bedrooms=None,
        actual_bedrooms=None,
        requested_bathrooms=1,
        actual_bathrooms=2,
        requested_floors=None,
        actual_floors=None,
    )
    assert result.passed is False
    assert "Bathroom count mismatch: client requested 1 bathroom(s), but design provides 2" in result.reason

def test_validate_preferences_combined():
    # D. combined preferences
    result = validate_preferences(
        requested_bedrooms=3,
        actual_bedrooms=3,
        requested_bathrooms=2,
        actual_bathrooms=2,
        requested_floors=1,
        actual_floors=1,
    )
    assert result.passed is True

def test_validate_preferences_bathroom_mismatch_others_match():
    # E. bathroom mismatch while bedrooms/floors match
    result = validate_preferences(
        requested_bedrooms=3,
        actual_bedrooms=3,
        requested_bathrooms=2,
        actual_bathrooms=1,
        requested_floors=1,
        actual_floors=1,
    )
    assert result.passed is False
    assert "Bathroom count mismatch" in result.reason

def test_validate_preferences_evidence_includes_bathroom_count():
    # F. expected/actual evidence includes bathroom count
    result = validate_preferences(
        requested_bedrooms=2,
        actual_bedrooms=2,
        requested_bathrooms=1,
        actual_bathrooms=1,
        requested_floors=1,
        actual_floors=1,
    )
    assert "Bathrooms=1" in result.expected
    assert "Bathrooms=1" in result.actual

def test_validate_preferences_existing_checks_unchanged():
    # G. existing bedroom and floor checks still work unchanged
    result = validate_preferences(
        requested_bedrooms=3,
        actual_bedrooms=2,
        requested_bathrooms=None,
        actual_bathrooms=None,
        requested_floors=2,
        actual_floors=1,
    )
    assert result.passed is False
    assert "Bedroom count mismatch" in result.reason
    assert "Floor count mismatch" in result.reason
