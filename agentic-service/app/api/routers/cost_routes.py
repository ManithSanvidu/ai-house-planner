"""Authenticated, non-persistent preview using the same calculator as generated designs."""

from datetime import datetime, timezone
from typing import Literal

from fastapi import APIRouter, HTTPException, Security
from pydantic import BaseModel, Field

from app.api.dependencies import verify_api_key
from app.cost.calculator import CostCalculationError, FORMULA_VERSION, calculate_cost_lines
from app.schemas.pricing_data import PricingItem

router = APIRouter()


class CostPreviewRequest(BaseModel):
    area_sqft: float = Field(gt=0, alias="areaSqft")
    terrain_type: Literal["flat", "hillside", "coastal"] = Field(alias="terrainType")
    pricing_items: list[PricingItem] = Field(alias="pricingItems")


@router.post("/cost/preview")
def preview_cost(request: CostPreviewRequest, _api_key: str = Security(verify_api_key)):
    try:
        lines, material, labour, total = calculate_cost_lines(
            request.area_sqft, request.terrain_type, request.pricing_items,
        )
    except CostCalculationError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc

    return {
        "materialCostLkr": material,
        "labourCostLkr": labour,
        "totalCostLkr": total,
        "budgetDeltaPercent": None,
        "breakdown": [
            {
                "itemName": line.item_name,
                "costHead": line.cost_head,
                "category": line.category,
                "unitCostLkr": line.unit_cost_lkr,
                "unit": line.unit,
                "appliedQuantity": line.applied_quantity,
                "quantityUnit": line.quantity_unit,
                "terrainMultiplier": line.terrain_multiplier,
                "amountLkr": line.amount_lkr,
                "sharePercent": line.share_percent,
                "provider": line.provider,
                "sourceReference": line.source_reference,
                "pricingUpdatedAt": line.pricing_updated_at,
            }
            for line in lines
        ],
        "formulaVersion": FORMULA_VERSION,
        "appliedAreaSqft": request.area_sqft,
        "terrainType": request.terrain_type,
        "estimatedAt": datetime.now(timezone.utc).isoformat(),
    }
