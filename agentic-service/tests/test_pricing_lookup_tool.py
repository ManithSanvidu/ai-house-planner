import pytest
import responses
import requests
from app.tools.pricing_lookup_tool import pricing_lookup_tool, PricingLookupError
from app.config import ASPNET_API_URL, INTERNAL_API_KEY


@responses.activate
def test_pricing_lookup_success():
    """Test successful retrieval and parsing of pricing data."""
    mock_data = [
        {
            "id": 1,
            "itemName": "Concrete Foundation",
            "category": "Foundation",
            "unitCostLkr": 5000.0,
            "unit": "sqft",
            "terrainMultiplier": {
                "flat": 1.0,
                "hillside": 1.25,
                "coastal": 1.35
            }
        },
        {
            "id": 2,
            "itemName": "Brick Wall",
            "category": "Walls",
            "unitCostLkr": 3500.0,
            "unit": "sqft",
            "terrainMultiplier": {
                "flat": 1.0,
                "hillside": 1.1,
                "coastal": 1.2
            }
        }
    ]

    responses.add(
        responses.GET,
        f"{ASPNET_API_URL}/internal/pricing",
        json=mock_data,
        status=200,
        headers={"Content-Type": "application/json"}
    )

    items = pricing_lookup_tool()

    assert len(items) == 2
    assert items[0].id == 1
    assert items[0].item_name == "Concrete Foundation"
    assert items[0].category == "Foundation"
    assert items[0].unit_cost_lkr == 5000.0
    assert items[0].unit == "sqft"
    assert items[0].terrain_multiplier.flat == 1.0
    assert items[0].terrain_multiplier.hillside == 1.25
    assert items[0].terrain_multiplier.coastal == 1.35

    assert items[1].id == 2
    assert items[1].item_name == "Brick Wall"
    assert items[1].unit_cost_lkr == 3500.0

    # Verify that X-Internal-API-Key was sent
    assert len(responses.calls) == 1
    assert responses.calls[0].request.headers.get("X-Internal-API-Key") == INTERNAL_API_KEY


@responses.activate
def test_pricing_lookup_empty_list_raises_error():
    """Test that empty pricing collection raises a controlled PricingLookupError without fallback."""
    responses.add(
        responses.GET,
        f"{ASPNET_API_URL}/internal/pricing",
        json=[],
        status=200,
        headers={"Content-Type": "application/json"}
    )

    with pytest.raises(PricingLookupError) as exc_info:
        pricing_lookup_tool()

    assert "empty" in str(exc_info.value).lower()


@responses.activate
def test_pricing_lookup_backend_server_error():
    """Test that backend 500 or 403 error raises a controlled PricingLookupError."""
    responses.add(
        responses.GET,
        f"{ASPNET_API_URL}/internal/pricing",
        body="Internal Server Error",
        status=500
    )

    with pytest.raises(PricingLookupError) as exc_info:
        pricing_lookup_tool()

    assert "500" in str(exc_info.value)


@responses.activate
def test_pricing_lookup_timeout_failure():
    """Test that request timeout raises a controlled PricingLookupError."""
    responses.add(
        responses.GET,
        f"{ASPNET_API_URL}/internal/pricing",
        body=requests.exceptions.Timeout("Connection timed out")
    )

    with pytest.raises(PricingLookupError) as exc_info:
        pricing_lookup_tool()

    assert "timed out" in str(exc_info.value).lower()


@responses.activate
def test_pricing_lookup_connection_failure():
    """Test that network connection failure raises a controlled PricingLookupError."""
    responses.add(
        responses.GET,
        f"{ASPNET_API_URL}/internal/pricing",
        body=requests.exceptions.ConnectionError("Failed to establish a new connection")
    )

    with pytest.raises(PricingLookupError) as exc_info:
        pricing_lookup_tool()

    assert "unable to connect" in str(exc_info.value).lower()


@responses.activate
def test_pricing_lookup_malformed_json():
    """Test that malformed JSON response raises a controlled PricingLookupError."""
    responses.add(
        responses.GET,
        f"{ASPNET_API_URL}/internal/pricing",
        body="<html><body>Bad Gateway</body></html>",
        status=200,
        headers={"Content-Type": "text/html"}
    )

    with pytest.raises(PricingLookupError) as exc_info:
        pricing_lookup_tool()

    assert "failed to parse" in str(exc_info.value).lower()
