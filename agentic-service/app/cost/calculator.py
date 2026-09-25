from collections.abc import Iterable

from app.schemas.cost_result import CostBreakdownLine
from app.schemas.pricing_data import PricingItem

FORMULA_VERSION = "category-area-v1"


class CostCalculationError(ValueError):
    pass


def calculate_cost_lines(
    area_sqft: float,
    terrain_type: str,
    pricing_items: Iterable[PricingItem],
) -> tuple[list[CostBreakdownLine], float, float, float]:
    """Calculate auditable cost heads without network or database access."""
    if area_sqft <= 0:
        raise CostCalculationError("Applied design area must be greater than zero.")
    items = list(pricing_items)
    materials = [i for i in items if i.category.strip().lower() == "material"]
    if not materials:
        raise CostCalculationError("At least one material pricing record is required.")
    for item in materials:
        if _unit(item.unit) not in {"per_sqft", "sqft"}:
            raise CostCalculationError(
                f"Material pricing item '{item.item_name}' has incompatible unit '{item.unit}'."
            )
    labour = [
        i for i in items
        if i.category.strip().lower() == "labour" and _unit(i.unit) in {"factor", "ratio"}
    ]
    if len(labour) != 1:
        raise CostCalculationError("Exactly one labour factor pricing record is required.")
    if labour[0].unit_cost_lkr <= 0:
        raise CostCalculationError("Labour factor must be greater than zero.")

    lines: list[CostBreakdownLine] = []
    material_total = 0.0
    for item in materials:
        multiplier = float(getattr(item.terrain_multiplier, terrain_type))
        amount = round(area_sqft * item.unit_cost_lkr * multiplier, 2)
        material_total += amount
        lines.append(CostBreakdownLine(
            item_name=item.item_name,
            cost_head=item.display_group or item.item_name.replace(" Materials", ""),
            category="material",
            unit_cost_lkr=item.unit_cost_lkr,
            unit=item.unit,
            applied_quantity=round(area_sqft, 2),
            quantity_unit="sq ft",
            terrain_multiplier=multiplier,
            amount_lkr=amount,
            provider=item.provider,
            source_reference=item.source_reference,
            pricing_updated_at=item.updated_at,
        ))
    material_total = round(material_total, 2)
    labour_total = round(material_total * labour[0].unit_cost_lkr, 2)
    total = round(material_total + labour_total, 2)
    lines.append(CostBreakdownLine(
        item_name=labour[0].item_name,
        cost_head=labour[0].display_group or "Labour",
        category="labour",
        unit_cost_lkr=labour[0].unit_cost_lkr,
        unit=labour[0].unit,
        applied_quantity=material_total,
        quantity_unit="material cost",
        terrain_multiplier=1.0,
        amount_lkr=labour_total,
        provider=labour[0].provider,
        source_reference=labour[0].source_reference,
        pricing_updated_at=labour[0].updated_at,
    ))
    for line in lines:
        line.share_percent = round(line.amount_lkr / total * 100, 2) if total else 0
    if lines:
        lines[-1].share_percent = round(lines[-1].share_percent + 100 - sum(x.share_percent for x in lines), 2)
    return lines, material_total, labour_total, total


def _unit(value: str) -> str:
    return value.strip().lower().replace(" ", "_").replace("-", "_")
