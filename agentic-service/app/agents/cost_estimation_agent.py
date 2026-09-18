"""
Cost Estimation Agent — LangGraph node (Component C).

Implements a fully deterministic cost calculation driven by:
  - Room geometry from state.design_result  (Component B output)
  - Live pricing from pricing_lookup_tool()  (ASP.NET pricing catalog)
  - Terrain multipliers from state.terrain_result (Component A output)
  - Project budget from state.input_data.budget_lkr

Formula
-------
  material_cost = Σ(room_area_sqft × material_unit_cost × terrain_multiplier)
  labour_cost   = material_cost × labour_rate_factor
  total_cost    = material_cost + labour_cost
  budget_delta_percent = (total_cost / budget_lkr) × 100

No database access, no hardcoded rates, no fallback prices.
"""
import logging
from datetime import datetime, timezone
from typing import List

from app.schemas.cost_result import CostResult
from app.schemas.pricing_data import PricingItem
from app.schemas.workflow_state import ExecutionLogEntry, WorkflowState
from app.tools.pricing_lookup_tool import PricingLookupError, pricing_lookup_tool

logger = logging.getLogger(__name__)

# Terrain types the agent accepts.  "unknown" is explicitly unsupported.
_SUPPORTED_TERRAINS = frozenset({"flat", "hillside", "coastal"})

# Units that express a per-square-foot cost and are therefore compatible with the
# room-area multiplication formula.
_AREA_COMPATIBLE_UNITS = frozenset({"per_sqft", "sqft"})

# Units that a labour-factor record must carry for the value to be treated as a
# dimensionless multiplier (not an LKR/day figure).
_LABOUR_FACTOR_UNITS = frozenset({"factor", "ratio"})


# ---------------------------------------------------------------------------
# Public node
# ---------------------------------------------------------------------------


def cost_estimation_node(state: WorkflowState) -> WorkflowState:
    """
    LangGraph node for deterministic cost estimation.

    On success  → populates state.cost_result and routes to 'validation'.
    On failure  → sets state.status = 'failed', state.current_agent = 'failed',
                  appends an ExecutionLogEntry, and returns without crashing.
    """
    print(f"[Cost Estimation Agent] Starting for workflow {state.workflow_id} …")

    try:
        result = _run_estimation(state)
    except _CostEstimationFailure as exc:
        return _fail(state, str(exc))

    state.cost_result = result.model_dump()
    state.current_agent = "validation"
    print(
        f"[Cost Estimation Agent] Done — total {result.total_cost_lkr:,.2f} LKR "
        f"({result.budget_delta_percent:.2f}% of budget)."
    )
    return state


# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------


class _CostEstimationFailure(Exception):
    """Controlled failure that aborts estimation without crashing the service."""


def _fail(state: WorkflowState, reason: str) -> WorkflowState:
    """Mark state as failed with a concise, actionable log entry."""
    print(f"[Cost Estimation Agent] FAILED — {reason}")
    logger.error("[Cost Estimation Agent] FAILED — %s", reason)
    state.status = "failed"
    state.current_agent = "failed"
    state.execution_log.append(
        ExecutionLogEntry(
            agent_name="CostEstimationAgent",
            action=f"Cost estimation failed: {reason}",
            tool_called="pricing_lookup_tool",
            result="failed",
            created_at_utc=datetime.now(timezone.utc).isoformat(),
        )
    )
    return state


def _run_estimation(state: WorkflowState) -> CostResult:
    """
    Core estimation logic.  Raises _CostEstimationFailure on any invalid input.
    """
    # ------------------------------------------------------------------
    # 1. Design data — rooms
    # ------------------------------------------------------------------
    if not state.design_result:
        raise _CostEstimationFailure(
            "Missing design_result: design agent must complete successfully before cost estimation."
        )

    rooms_raw = state.design_result.get("rooms")
    if not rooms_raw:
        raise _CostEstimationFailure(
            "design_result contains no rooms: cannot calculate material cost without room geometry."
        )

    room_areas: List[float] = []
    for idx, room in enumerate(rooms_raw):
        area = _extract_room_area(room, idx)
        room_areas.append(area)

    total_area_sqft = sum(room_areas)

    # ------------------------------------------------------------------
    # 2. Terrain — authoritative source is state.terrain_result
    # ------------------------------------------------------------------
    terrain_type = _resolve_terrain(state)

    # ------------------------------------------------------------------
    # 3. Pricing lookup
    # ------------------------------------------------------------------
    try:
        pricing_items: List[PricingItem] = pricing_lookup_tool()
    except PricingLookupError as exc:
        raise _CostEstimationFailure(
            f"Pricing lookup failed; cannot proceed without live pricing data: {exc}"
        ) from exc

    # ------------------------------------------------------------------
    # 4. Material items — filter and validate units
    # ------------------------------------------------------------------
    material_items = [
        item for item in pricing_items if item.category.strip().lower() == "material"
    ]
    if not material_items:
        raise _CostEstimationFailure(
            "No pricing items with category='material' found: at least one is required."
        )

    for item in material_items:
        unit_norm = item.unit.strip().lower().replace(" ", "_").replace("-", "_")
        if unit_norm not in _AREA_COMPATIBLE_UNITS:
            raise _CostEstimationFailure(
                f"Material pricing item '{item.item_name}' (id={item.id}) has incompatible unit "
                f"'{item.unit}'. Only per-sqft units ({sorted(_AREA_COMPATIBLE_UNITS)}) may be "
                f"multiplied by room area. Resolve the pricing catalog entry before continuing."
            )

    # ------------------------------------------------------------------
    # 5. Labour factor — exactly one record with a dimensionless unit
    # ------------------------------------------------------------------
    labour_factor = _resolve_labour_factor(pricing_items)

    # ------------------------------------------------------------------
    # 6. Terrain multiplier
    # ------------------------------------------------------------------
    # TerrainMultiplier is a Pydantic model; access via attribute.
    terrain_multiplier = _get_terrain_multiplier_for_items(material_items, terrain_type)

    # ------------------------------------------------------------------
    # 7. Material cost
    # ------------------------------------------------------------------
    material_cost = 0.0
    for room_area in room_areas:
        for item in material_items:
            multiplier = _terrain_multiplier_value(item, terrain_type)
            material_cost += room_area * item.unit_cost_lkr * multiplier

    # ------------------------------------------------------------------
    # 8. Labour cost
    # ------------------------------------------------------------------
    labour_cost = material_cost * labour_factor

    # ------------------------------------------------------------------
    # 9. Budget
    # ------------------------------------------------------------------
    budget_lkr = state.input_data.budget_lkr if state.input_data else None
    if not budget_lkr or budget_lkr <= 0:
        raise _CostEstimationFailure(
            f"Invalid budget_lkr={budget_lkr!r}: a positive budget is required to calculate "
            "budget_delta_percent."
        )

    total_cost = material_cost + labour_cost
    budget_delta_percent = (total_cost / budget_lkr) * 100

    # ------------------------------------------------------------------
    # 10. Build structured result
    # ------------------------------------------------------------------
    return CostResult(
        material_cost_lkr=round(material_cost, 2),
        labour_cost_lkr=round(labour_cost, 2),
        total_cost_lkr=round(total_cost, 2),
        budget_delta_percent=round(budget_delta_percent, 2),
        terrain_type=terrain_type,
        room_count=len(room_areas),
        total_area_sqft=round(total_area_sqft, 2),
    )


# ---------------------------------------------------------------------------
# Room area extraction
# ---------------------------------------------------------------------------


def _extract_room_area(room: dict, idx: int) -> float:
    """
    Return the usable area (sqft) for a single room dict.

    Preference order:
      1. Explicit 'area_sqft' field (if present and positive).
      2. Computed width × length (both must be present and positive).

    Raises _CostEstimationFailure for any invalid/missing geometry.
    """
    room_label = room.get("room_type", room.get("name", f"room[{idx}]"))

    area = room.get("area_sqft")
    if area is not None:
        try:
            area = float(area)
        except (TypeError, ValueError):
            raise _CostEstimationFailure(
                f"Room '{room_label}': area_sqft={area!r} is not a valid number."
            )
        if area <= 0:
            raise _CostEstimationFailure(
                f"Room '{room_label}': area_sqft={area} is zero or negative; "
                "cannot use this room for cost calculation."
            )
        return area

    # Fall back to width × length
    width = room.get("width")
    length = room.get("length")
    if width is None or length is None:
        raise _CostEstimationFailure(
            f"Room '{room_label}': neither area_sqft nor width/length dimensions are present."
        )
    try:
        width = float(width)
        length = float(length)
    except (TypeError, ValueError):
        raise _CostEstimationFailure(
            f"Room '{room_label}': width={width!r} or length={length!r} is not a valid number."
        )
    if width <= 0 or length <= 0:
        raise _CostEstimationFailure(
            f"Room '{room_label}': width={width} and length={length} must both be positive; "
            "zero or negative dimensions are rejected."
        )
    return width * length


# ---------------------------------------------------------------------------
# Terrain resolution
# ---------------------------------------------------------------------------


def _resolve_terrain(state: WorkflowState) -> str:
    """
    Extract terrain_type from state.terrain_result.

    state.terrain_result is stored as a plain dict (model_dump() in the land agent).
    Raises _CostEstimationFailure if missing or unsupported.
    """
    if not state.terrain_result:
        raise _CostEstimationFailure(
            "state.terrain_result is None: land analysis agent must complete before cost estimation. "
            "Cannot select terrain multiplier without a classified terrain type."
        )

    terrain_type = state.terrain_result.get("terrain_type")
    if not terrain_type:
        raise _CostEstimationFailure(
            "state.terrain_result is present but 'terrain_type' key is missing or empty."
        )

    terrain_norm = str(terrain_type).strip().lower()
    if terrain_norm not in _SUPPORTED_TERRAINS:
        raise _CostEstimationFailure(
            f"Unsupported terrain_type='{terrain_type}'. "
            f"Accepted values: {sorted(_SUPPORTED_TERRAINS)}. "
            "Update the land analysis result before running cost estimation."
        )

    return terrain_norm


# ---------------------------------------------------------------------------
# Labour factor resolution
# ---------------------------------------------------------------------------


def _resolve_labour_factor(pricing_items: List[PricingItem]) -> float:
    """
    Find exactly one labour pricing item whose unit is 'factor' or 'ratio'.

    Returns the dimensionless multiplier value.
    Raises _CostEstimationFailure if the result is ambiguous or missing.
    """
    labour_items = [
        item for item in pricing_items if item.category.strip().lower() == "labour"
    ]

    factor_items = [
        item
        for item in labour_items
        if item.unit.strip().lower().replace(" ", "_").replace("-", "_")
        in _LABOUR_FACTOR_UNITS
    ]

    if not factor_items:
        non_factor_units = [i.unit for i in labour_items]
        raise _CostEstimationFailure(
            f"No labour pricing item with a dimensionless unit ({sorted(_LABOUR_FACTOR_UNITS)}) "
            f"found. Labour items present have units: {non_factor_units}. "
            "A single 'factor' or 'ratio' record is required to compute labour cost."
        )

    if len(factor_items) > 1:
        names = [i.item_name for i in factor_items]
        raise _CostEstimationFailure(
            f"Ambiguous labour factor: {len(factor_items)} items qualify ({names}). "
            "Exactly one labour-factor pricing record is required."
        )

    factor_item = factor_items[0]
    factor_value = factor_item.unit_cost_lkr
    if factor_value <= 0:
        raise _CostEstimationFailure(
            f"Labour factor item '{factor_item.item_name}' has unit_cost_lkr={factor_value}, "
            "which is not a positive multiplier."
        )

    return factor_value


# ---------------------------------------------------------------------------
# Terrain multiplier value access (PricingItem is a Pydantic model)
# ---------------------------------------------------------------------------


def _terrain_multiplier_value(item: PricingItem, terrain_type: str) -> float:
    """
    Return the terrain multiplier for the given terrain_type from a PricingItem.

    item.terrain_multiplier is a TerrainMultiplier Pydantic model instance,
    so attributes are accessed directly (not via dict subscript).
    """
    tm = item.terrain_multiplier  # TerrainMultiplier instance
    if terrain_type == "flat":
        return tm.flat
    elif terrain_type == "hillside":
        return tm.hillside
    elif terrain_type == "coastal":
        return tm.coastal
    else:
        # Should never reach here because terrain was validated before this call
        raise _CostEstimationFailure(
            f"Internal error: terrain_type='{terrain_type}' reached multiplier lookup "
            "but was not caught by earlier validation."
        )


def _get_terrain_multiplier_for_items(
    material_items: List[PricingItem], terrain_type: str
) -> float:
    """
    Not used in the summation loop (each item has its own multiplier),
    but kept as a sanity-check helper for tests that want a single scalar.
    Returns the terrain multiplier of the first material item.
    """
    return _terrain_multiplier_value(material_items[0], terrain_type)