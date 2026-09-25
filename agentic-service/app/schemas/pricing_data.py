from typing import Any

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
    display_group: str | None = Field(None, alias="displayGroup")
    unit_cost_lkr: float = Field(..., alias="unitCostLkr")
    unit: str
    terrain_multiplier: TerrainMultiplier = Field(default_factory=TerrainMultiplier, alias="terrainMultiplier")
    region: str = "Sri Lanka"
    quality_level: str = Field("Standard", alias="qualityLevel")
    is_active: bool = Field(True, alias="isActive")
    provider: str | None = None
    source_reference: str | None = Field(None, alias="sourceReference")
    updated_at: str | None = Field(None, alias="updatedAt")

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
                "displaygroup": "display_group",
                "display_group": "display_group",
                "unitcostlkr": "unit_cost_lkr",
                "unit_cost_lkr": "unit_cost_lkr",
                "unit": "unit",
                "terrainmultiplier": "terrain_multiplier",
                "terrain_multiplier": "terrain_multiplier",
                "region": "region",
                "qualitylevel": "quality_level",
                "quality_level": "quality_level",
                "isactive": "is_active",
                "is_active": "is_active",
                "provider": "provider",
                "sourcereference": "source_reference",
                "source_reference": "source_reference",
                "updatedat": "updated_at",
                "updated_at": "updated_at",
            }
            for k, v in data.items():
                k_clean = str(k).replace("_", "").lower()
                target_key = key_map.get(str(k).lower(), key_map.get(k_clean, k))
                normalized[target_key] = v
            return normalized
        return data
