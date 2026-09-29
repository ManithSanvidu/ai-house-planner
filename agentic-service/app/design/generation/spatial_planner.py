"""Deterministic room-program construction for the simplified customer intake."""

from typing import Any

from app.design.geometry.plot_constraints import PlotConstraints
from app.design.program.models import Requirements
from app.design.program.spatial_program import (
    AdjacencyIntent,
    EntranceIntent,
    RoomIntent,
    SpatialProgram,
)

FIXED_PROGRAM_MESSAGE = (
    "The room program is fixed by the deterministic planner. "
    "You must preserve the provided room list exactly."
)


def _room(room_id: str, room_type: str, zone: str, target: int, minimum: int,
          position: str, exterior: bool, privacy: str) -> RoomIntent:
    return RoomIntent(id=room_id, type=room_type, floor=1, zone=zone,
                      target_area_sqft=target, min_area_sqft=minimum,
                      preferred_position=position, exterior_wall_required=exterior,
                      privacy_level=privacy)


def plan_spatial_program(req: Requirements, plot: PlotConstraints,
                         workflow_id: object | None = None) -> tuple[SpatialProgram, dict[str, Any]]:
    """Build the exact one-floor room list; no provider decides rooms or floors."""
    if req.floors != 1 or not 1 <= req.bedrooms <= 3 or not 1 <= req.bathrooms <= 2:
        raise ValueError("The selected house requirements are not practical for this land size.")

    rooms = [
        _room("living_room", "living_room", "PUBLIC", 220, 160, "FRONT", True, "LOW"),
        _room("kitchen", "kitchen", "SERVICE", 130, 90, "REAR_LEFT", True, "MEDIUM"),
        _room("dining_area", "dining_area", "PUBLIC", 120, 80, "CENTER", False, "LOW"),
    ]
    bedroom_positions = ("REAR_RIGHT", "REAR", "RIGHT")
    for index in range(req.bedrooms):
        rooms.append(_room(f"bedroom_{index + 1}", "bedroom", "PRIVATE", 140, 100,
                           bedroom_positions[index], True, "HIGH"))
    bathroom_positions = ("CENTER", "LEFT")
    for index in range(req.bathrooms):
        rooms.append(_room(f"bathroom_{index + 1}", "bathroom", "SERVICE", 55, 40,
                           bathroom_positions[index], False, "HIGH"))

    adjacencies = [
        AdjacencyIntent(room_a="living_room", room_b="dining_area", relationship="ADJACENT", priority="HIGH"),
        AdjacencyIntent(room_a="dining_area", room_b="kitchen", relationship="ADJACENT", priority="HIGH"),
    ]
    for index in range(req.bedrooms):
        adjacencies.append(AdjacencyIntent(room_a=f"bedroom_{index + 1}", room_b="living_room",
                                           relationship="NEAR", priority="MEDIUM"))
    for index in range(req.bathrooms):
        adjacent_bedroom = min(index + 1, req.bedrooms)
        adjacencies.append(AdjacencyIntent(room_a=f"bathroom_{index + 1}",
                                           room_b=f"bedroom_{adjacent_bedroom}",
                                           relationship="NEAR", priority="HIGH"))

    program = SpatialProgram(
        concept=f"{req.style.title()} single-floor family home",
        floor_count=1,
        rooms=rooms,
        adjacencies=adjacencies,
        entrance=EntranceIntent(preferred_side="SOUTH", connect_to="living_room"),
        vertical_core=None,
        reason_codes=["FIXED_ROOM_PROGRAM", "SINGLE_FLOOR"],
    )
    return program, {
        "provider": "deterministic",
        "model": None,
        "provider_call_count": 0,
        "room_count": len(rooms),
        "total_target_area": sum(room.target_area_sqft for room in rooms),
    }


def _validate_spatial_program(program: SpatialProgram, req: Requirements,
                              area_budget_sqft: float) -> None:
    """Compatibility helper retained for direct validation tests."""
    bedrooms = [room for room in program.rooms if "bedroom" in room.type.lower()]
    bathrooms = [room for room in program.rooms if "bath" in room.type.lower()]
    if len(bedrooms) != req.bedrooms:
        raise ValueError(f"Expected {req.bedrooms} bedrooms, got {len(bedrooms)}.")
    if len(bathrooms) != req.bathrooms:
        raise ValueError(f"Expected {req.bathrooms} bathrooms, got {len(bathrooms)}.")
    if program.floor_count != req.floors:
        raise ValueError(f"Expected {req.floors} floors, got {program.floor_count}.")
    if sum(room.target_area_sqft for room in program.rooms) > area_budget_sqft * 1.1:
        raise ValueError("Programmed area exceeds the buildable area budget.")
