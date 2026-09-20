from typing import Any, List, Optional
from pydantic import BaseModel, ConfigDict, Field, model_validator


class TerrainMultiplier(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    flat: float = 1.0
    hillside: float = 1.25
    coastal: float = 1.35

    @model_validator(mode="before")
    @classmethod
    def _normalize_keys(cls, data: Any) -> Any:
        if isinstance(data, dict):
            return {str(k).lower(): v for k, v in data.items()}
        return data


class PricingItem(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: int
    item_name: str = Field(..., alias="itemName")
    category: str
    unit_cost_lkr: float = Field(..., alias="unitCostLkr")
    unit: str
    terrain_multiplier: TerrainMultiplier = Field(default_factory=TerrainMultiplier, alias="terrainMultiplier")

    @model_validator(mode="before")
    @classmethod
    def _normalize_keys(cls, data: Any) -> Any:
        if isinstance(data, dict):
            normalized = {}
            key_map = {
                "id": "id",
                "itemname": "item_name",
                "item_name": "item_name",
                "category": "category",
                "unitcostlkr": "unit_cost_lkr",
                "unit_cost_lkr": "unit_cost_lkr",
                "unit": "unit",
                "terrainmultiplier": "terrain_multiplier",
                "terrain_multiplier": "terrain_multiplier",
            }
            for k, v in data.items():
                k_clean = str(k).replace("_", "").lower()
                target_key = key_map.get(str(k).lower(), key_map.get(k_clean, k))
                normalized[target_key] = v
            return normalized
        return data
