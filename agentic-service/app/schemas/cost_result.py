"""
Structured result schema for the Cost Estimation Agent (Component C).

Stored in WorkflowState.cost_result as a plain dict (via .model_dump()) so that
the WorkflowState type annotation (Dict[str, Any]) is preserved without modification.
"""
from pydantic import BaseModel, Field


class CostResult(BaseModel):
    """
    Deterministic cost breakdown produced by the Cost Estimation Agent.

    All monetary values are in Sri Lankan Rupees (LKR).
    budget_delta_percent expresses total_cost as a percentage of the submitted budget
    (100 = exactly on budget, >100 = over budget, <100 = under budget).
    """

    material_cost_lkr: float = Field(
        ...,
        description="Sum of (room_area × material_unit_cost × terrain_multiplier) across all rooms and material items.",
    )
    labour_cost_lkr: float = Field(
        ...,
        description="material_cost_lkr × labour_rate_factor.",
    )
    total_cost_lkr: float = Field(
        ...,
        description="material_cost_lkr + labour_cost_lkr.",
    )
    budget_delta_percent: float = Field(
        ...,
        description="(total_cost_lkr / budget_lkr) × 100, rounded to 2 decimal places.",
    )
    terrain_type: str = Field(
        ...,
        description="Terrain type used for multiplier selection (flat / hillside / coastal).",
    )
    room_count: int = Field(
        ...,
        ge=1,
        description="Number of rooms included in the calculation.",
    )
    total_area_sqft: float = Field(
        ...,
        gt=0,
        description="Sum of all room areas used in the calculation.",
    )
