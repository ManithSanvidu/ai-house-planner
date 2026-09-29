import json
import logging
import time
from typing import Any

from app.design.program.models import Requirements
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.program.spatial_program import SpatialProgram
from app.design.exceptions import GenerationFailure
from app.providers import get_available_design_provider
from app.cache import AIResponseCache, spatial_program_cache_key

logger = logging.getLogger(__name__)

SYSTEM_PROMPT = """Create a semantic house SpatialProgram matching the supplied counts and requirements.
Do not create coordinates. Keep rooms within a reasonable area for the land size. Put public rooms
near the entrance, private rooms away from it, and group service/wet rooms. Use valid floors and
semantic positions. Return only JSON matching the schema."""

def _build_prompt(req: Requirements, plot: PlotConstraints, area_budget_sqft: float) -> str:
    requirements = {
        "bedrooms": req.bedrooms,
        "bathrooms": req.bathrooms,
        "open_plan": req.open_plan,
        "separate_dining": req.dining_required,
        "master_bedroom": req.master_bedroom,
        "home_office": req.home_office,
        "parking": req.parking,
        "accessibility": req.accessibility,
        "utility_room": req.utility_room,
        "space_priority": req.space_priority,
    }
    payload = {
        "bedrooms": req.bedrooms,
        "bathrooms": req.bathrooms,
        "floors": req.floors,
        "style": req.style,
        "land_size": plot.land_size_perches,
        "requirements": requirements,
    }
    return json.dumps(payload, separators=(',', ':'))


def _validate_spatial_program(program: SpatialProgram, req: Requirements, area_budget_sqft: float) -> None:
    # 1. Floor count match
    if program.floor_count != req.floors:
        raise ValueError(f"Expected {req.floors} floors, got {program.floor_count}.")
    
    # 2. Room counts
    bedrooms = [r for r in program.rooms if "bedroom" in r.type.lower()]
    bathrooms = [r for r in program.rooms if "bathroom" in r.type.lower() or "bath" in r.type.lower()]
    if len(bedrooms) != req.bedrooms:
        raise ValueError(f"Expected {req.bedrooms} bedrooms, got {len(bedrooms)}.")
    if len(bathrooms) != req.bathrooms:
        raise ValueError(f"Expected {req.bathrooms} bathrooms, got {len(bathrooms)}.")
    
    # 3. Features
    if req.dining_required and not any("dining" in r.type.lower() for r in program.rooms):
        raise ValueError("Requested separate dining but no dining room provided.")
    if req.home_office and not any("office" in r.type.lower() for r in program.rooms):
        raise ValueError("Requested home office but none provided.")
    
    # 4. Valid floors
    for room in program.rooms:
        if room.floor < 1 or room.floor > program.floor_count:
            raise ValueError(f"Room {room.id} assigned to invalid floor {room.floor}.")
            
    # 5. Adjacency references and unique IDs
    room_ids = set()
    for r in program.rooms:
        if r.id in room_ids:
            raise ValueError(f"Duplicate room ID '{r.id}'.")
        room_ids.add(r.id)
        
    for adj in program.adjacencies:
        if adj.room_a not in room_ids:
            raise ValueError(f"Adjacency references unknown room '{adj.room_a}'.")
        if adj.room_b not in room_ids:
            raise ValueError(f"Adjacency references unknown room '{adj.room_b}'.")
            
    # 6. Area budget
    total_target_area = sum(r.target_area_sqft for r in program.rooms)
    if total_target_area > area_budget_sqft * 1.1: # 10% leniency
        raise ValueError(f"Programmed area {total_target_area} exceeds budget {area_budget_sqft}.")
    if total_target_area <= 0:
        raise ValueError("Programmed area must be positive.")


def plan_spatial_program(req: Requirements, plot: PlotConstraints, workflow_id: object | None = None) -> tuple[SpatialProgram, dict[str, Any]]:
    # 1. Area budget calculation
    # Using 65% coverage limit (max_buildable_area from land_math)
    # Total available area depends on floors
    from app.land.land_math import max_buildable_area
    max_ground = min(max_buildable_area(plot.land_size_perches), plot.buildable_width * plot.buildable_length)
    area_budget_sqft = max_ground * req.floors * 0.9  # 90% of max theoretical box

    user_prompt = _build_prompt(req, plot, area_budget_sqft)
    
    # Track prompt size
    prompt_chars = len(user_prompt)
    logger.info("[SpatialPlanner] request_chars=%d", prompt_chars)

    provider = get_available_design_provider()
    if not provider:
        raise GenerationFailure("No LLM provider available for spatial planning.")

    start_time = time.time()
    cache = AIResponseCache()
    cache_key = spatial_program_cache_key(req, plot.land_size_perches)
    raw_json = cache.get(cache_key) if provider.provider_name == "openai" else None
    cache_hit = raw_json is not None
    try:
        if raw_json is not None:
            print("[AI CACHE] HIT")
        else:
            print("[AI CACHE] MISS")
            if provider.provider_name == "openai" and workflow_id is not None:
                from app.services.ai_guard import execute_once
                raw_json, _ = execute_once(
                    workflow_id,
                    "design_strategy",
                    lambda: provider.generate_json(SYSTEM_PROMPT, user_prompt, SpatialProgram, max_tokens=1500),
                )
            else:
                raw_json = provider.generate_json(SYSTEM_PROMPT, user_prompt, SpatialProgram, max_tokens=1500)
    except Exception as exc:
        raise GenerationFailure(f"Spatial planning failed: {exc}") from exc
        
    latency_ms = int((time.time() - start_time) * 1000)

    try:
        program = SpatialProgram.model_validate(raw_json)
        _validate_spatial_program(program, req, area_budget_sqft)
        if provider.provider_name == "openai" and not cache_hit:
            cache.set(cache_key, raw_json)
    except ValueError as exc:
        raise GenerationFailure(f"Validation failed on generated spatial program: {exc}") from exc

    metadata = {
        "provider": provider.provider_name,
        "model": provider.model_name,
        "latency_ms": latency_ms,
        "prompt_chars": prompt_chars,
        "area_budget_sqft": round(area_budget_sqft, 1),
        "total_target_area": sum(r.target_area_sqft for r in program.rooms),
        "cache_hit": cache_hit,
    }
    
    return program, metadata
