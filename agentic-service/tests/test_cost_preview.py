from fastapi.testclient import TestClient

from app.config import INTERNAL_API_KEY
from app.main import app


def _request():
    return {
        "areaSqft": 100,
        "terrainType": "flat",
        "pricingItems": [
            {"id": 1, "itemName": "Foundation Materials", "displayGroup": "Foundation",
             "category": "material", "unitCostLkr": 3000, "unit": "per_sqft",
             "terrainMultiplier": {"flat": 1, "hillside": 1.15, "coastal": 1.1}},
            {"id": 2, "itemName": "Construction Labour", "displayGroup": "Labour",
             "category": "labour", "unitCostLkr": .35, "unit": "factor",
             "terrainMultiplier": {"flat": 1, "hillside": 1, "coastal": 1}},
        ],
    }


def test_catalogue_preview_uses_the_generated_design_formula():
    client = TestClient(app)
    response = client.post("/cost/preview", json=_request(),
                           headers={"X-Internal-API-Key": INTERNAL_API_KEY})
    assert response.status_code == 200
    result = response.json()
    assert result["materialCostLkr"] == 300_000
    assert result["labourCostLkr"] == 105_000
    assert result["totalCostLkr"] == 405_000
    assert result["formulaVersion"] == "category-area-v1"
    assert sum(line["amountLkr"] for line in result["breakdown"]) == result["totalCostLkr"]


def test_catalogue_preview_requires_internal_auth_and_rejects_overlap():
    client = TestClient(app)
    assert client.post("/cost/preview", json=_request()).status_code == 401
    request = _request()
    request["pricingItems"].insert(1, {
        "id": 3, "itemName": "Tiles", "displayGroup": "Foundation",
        "category": "material", "unitCostLkr": 100, "unit": "per_sqft",
        "terrainMultiplier": {"flat": 1, "hillside": 1, "coastal": 1},
    })
    response = client.post("/cost/preview", json=request,
                           headers={"X-Internal-API-Key": INTERNAL_API_KEY})
    assert response.status_code == 422
