from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class HouseRequirement(BaseModel):
    """The complete customer-controlled generation contract."""

    model_config = ConfigDict(extra="forbid")
    land_size_category: Literal["small", "medium"]
    land_size_perches: float = Field(ge=10, le=35)
    bedrooms: int = Field(ge=1, le=3)
    bathrooms: int = Field(ge=1, le=2)
    house_type: Literal["simple", "modern"]
    floors: Literal[1] = 1
