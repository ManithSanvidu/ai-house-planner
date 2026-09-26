"""
Validation schemas for Component D — Validation/Safety Agent.
Provides strongly typed Pydantic models for rule-by-rule and overall validation results.
"""

from typing import Any

from pydantic import BaseModel, Field


class RuleValidationResult(BaseModel):
    """Result of an individual validation rule evaluation."""
    rule_name: str = Field(..., description="Unique identifier of the validation rule")
    passed: bool = Field(..., description="Whether the rule passed or failed")
    reason: str = Field(..., description="Human-readable explanation of the validation result")
    actual: Any | None = Field(None, description="Actual value extracted from design/cost/terrain data")
    expected: Any | None = Field(None, description="Expected value or acceptable range/threshold")


class ValidationResult(BaseModel):
    """Structured validation outcome produced by the Validation/Safety Agent."""
    passed: bool = Field(..., description="Overall validation status (True only if all rules pass)")
    rules: list[RuleValidationResult] = Field(default_factory=list, description="Detailed results for each rule")
    errors: list[str] = Field(default_factory=list, description="List of critical error messages or failure reasons")
    summary: str = Field("", description="Summary overview of the validation outcome")
    revision_reason: str | None = Field(None, description="Compiled revision reason for downstream design agent")


class HousePlanValidationInput(BaseModel):
    """
    Standardized input payload for the Validation/Safety Agent.
    Can be constructed directly or extracted from WorkflowState.
    """
    land_size_perches: float | None = Field(None, description="Land size in perches (1 perch = 272.25 sqft)")
    ground_coverage_sqft: float | None = Field(None, description="Building footprint / ground coverage in sqft")
    terrain_type: str | None = Field(None, description="Terrain type (e.g., flat, slope, rocky)")
    slope_estimate: str | None = Field(None, description="Slope estimate (e.g., flat, moderate_slope, steep_slope)")
    foundation_type: str | None = Field(None, description="Proposed foundation type (e.g., strip, raft, stepped_strip, pile)")
    budget_lkr: float | None = Field(None, description="Client stated budget in LKR")
    estimated_cost_lkr: float | None = Field(None, description="Total estimated construction cost in LKR")
    requested_bedrooms: int | None = Field(None, description="Bedrooms requested by the client")
    actual_bedrooms: int | None = Field(None, description="Bedrooms provided in the architectural design")
    requested_floors: int | None = Field(None, description="Floors requested by the client")
    actual_floors: int | None = Field(None, description="Floors provided in the architectural design")
    additional_data: dict[str, Any] | None = Field(default_factory=dict, description="Any extra metadata")
