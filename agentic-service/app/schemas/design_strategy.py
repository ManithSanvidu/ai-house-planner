from typing import Literal, Optional
from pydantic import BaseModel, Field

class LandSummary(BaseModel):
    perches: float
    area_sqft: float
    width_ft: float
    length_ft: float
    buildable_width_ft: float
    buildable_length_ft: float
    terrain: str
    road_side: str

class DesignStrategyRequest(BaseModel):
    land: LandSummary
    requirements: dict  # A compact dict of essential Requirements
    eligible_families: list[str]

class DesignStrategy(BaseModel):
    """
    High-level conceptual strategy returned by the LLM. 
    Does not contain exact coordinates.
    """
    topology_family: Literal[
        "LINEAR",
        "COMPACT_RECTANGLE",
        "L_SHAPE",
        "T_SHAPE",
        "CENTRAL_CORE",
        "SPLIT_ZONE",
        "DUPLEX_STACKED",
        "HILLSIDE_STEPPED",
        "COASTAL_RAISED_COMPACT"
    ] = Field(description="The topological family driving the overall shape and zoning.")
    
    public_zone: str = Field(description="Strategy for the public anchor (Living/Dining).")
    private_zone: str = Field(description="Strategy for the bedroom cluster.")
    service_zone: str = Field(description="Strategy for kitchen, utility, and bathrooms.")
    
    circulation_strategy: Literal[
        "direct",
        "short_central_hall",
        "central_lobby",
        "split_lobby"
    ] = Field(description="How to route movement through the house.")
    
    entrance_side: Literal["north", "south", "east", "west"] = Field(description="Direction the main entrance faces.")
    
    bedroom_strategy: str = Field(description="How bedrooms relate to each other for privacy.")
    kitchen_relationship: str = Field(description="How the kitchen connects to living/dining vs service areas.")
    wet_zone_strategy: str = Field(description="Consolidation of plumbing walls if applicable.")
    style_intent: str = Field(description="General architectural styling implications.")
    garden_orientation: Optional[str] = Field(None, description="Which side the primary outdoor space is favored.")
