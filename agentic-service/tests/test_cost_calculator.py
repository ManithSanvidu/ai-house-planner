import pytest

from app.cost.calculator import CostCalculationError, calculate_cost_lines
from app.schemas.pricing_data import PricingItem


def _item(item_id: int, name: str, category: str, unit: str, rate: float) -> PricingItem:
    return PricingItem.model_validate({
        "id": item_id,
        "itemName": name,
        "displayGroup": name.replace(" Materials", ""),
        "category": category,
        "unitCostLkr": rate,
        "unit": unit,
        "terrainMultiplier": {"flat": 1, "hillside": 1.15, "coastal": 1.10},
        "provider": "Manual",
        "sourceReference": "Contractor benchmark",
        "updatedAt": "2026-09-24T00:00:00Z",
    })


def test_calculator_returns_auditable_lines_and_balanced_total():
    lines, materials, labour, total = calculate_cost_lines(100, "flat", [
        _item(1, "Foundation Materials", "material", "per_sqft", 3000),
        _item(2, "Construction Labour", "labour", "factor", .35),
    ])
    assert materials == 300_000
    assert labour == 105_000
    assert total == 405_000
    assert sum(line.amount_lkr for line in lines) == total
    assert sum(line.share_percent for line in lines) == 100
    assert lines[0].provider == "Manual"


def test_calculator_applies_terrain_to_each_material_head():
    _, materials, _, _ = calculate_cost_lines(100, "hillside", [
        _item(1, "Foundation Materials", "material", "per_sqft", 3000),
        _item(2, "Construction Labour", "labour", "factor", .35),
    ])
    assert materials == 345_000


def test_calculator_rejects_non_area_material_unit():
    with pytest.raises(CostCalculationError, match="incompatible unit"):
        calculate_cost_lines(100, "flat", [
            _item(1, "Cement", "material", "bag", 2500),
            _item(2, "Construction Labour", "labour", "factor", .35),
        ])
