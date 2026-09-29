import logging
import os
import base64
import uuid
import requests
from .openai_visualization_service import OpenAIVisualizationService
from app.config import (
    AGENTIC_PUBLIC_BASE_URL,
    ASPNET_API_URL,
    ENABLE_AI_VISUALIZATION,
    INTERNAL_API_KEY,
    VISUALIZATIONS_DIR,
)
from app.services.ai_guard import DuplicateAIRequest, execute_once

logger = logging.getLogger(__name__)
ROOM_VALIDATION_ERROR = "Generated layout failed room requirement validation."


def _room_program_matches(rooms: list[dict], bedrooms: int, bathrooms: int) -> bool:
    bedroom_count = sum(1 for room in rooms if "bedroom" in str(room.get("room_type", "")).lower())
    bathroom_count = sum(1 for room in rooms if "bath" in str(room.get("room_type", "")).lower())
    required = ("living", "kitchen", "dining")
    types = [str(room.get("room_type", "")).lower() for room in rooms]
    return (bedroom_count == bedrooms and bathroom_count == bathrooms and
            all(any(name in room_type for room_type in types) for name in required))

class VisualizationAgent:
    def __init__(self, api_key: str = None, workflow_id: str | None = None):
        self.api_key = api_key or os.getenv("OPENAI_API_KEY")
        self.workflow_id = workflow_id
        self.service = OpenAIVisualizationService(api_key=self.api_key, workflow_id=workflow_id)

    def process(self, validated_layout: dict) -> dict:
        """
        Accepts a validated layout JSON and returns a visualization response dict.
        The service layer checks ENABLE_OPENAI and the daily spend cap before calling OpenAI.
        """
        result = self.service.generate_visualization(validated_layout)
        return result

def visualization_node(state):
    import logging
    logger = logging.getLogger(__name__)
    print("[Visualization Agent] Starting visualization")
    logger.info("[Visualization Agent] Starting visualization")
    
    if not getattr(state, "design_result", None):
        print("[Visualization Agent] No design_result found!")
        return state
        
    rooms = getattr(state, "design_result", {}).get("rooms", [])
    if isinstance(getattr(state, "design_result", None), str):
        print("[Visualization Agent] design_result is a string?!")
        import json
        rooms = json.loads(state.design_result).get("rooms", [])

    print(f"[Visualization Agent] Layout received: {len(rooms)} rooms")
    logger.info(f"[Visualization Agent] Layout received: {len(rooms)} rooms")
    
    expected_bedrooms = getattr(getattr(state, "input_data", None), "bedrooms", None)
    expected_bathrooms = getattr(getattr(state, "input_data", None), "bathrooms", None)
    if (expected_bedrooms is None or expected_bathrooms is None or
            not _room_program_matches(rooms, expected_bedrooms, expected_bathrooms)):
        logger.error("[Visualization Agent] %s", ROOM_VALIDATION_ERROR)
        state.design_result["ai_visualization"] = {
            "image_url": None, "status": "failed", "error": ROOM_VALIDATION_ERROR
        }
        _persist_visualization_status(state.workflow_id, "failed")
        return state

    existing = _existing_visualization(state.workflow_id)
    if existing:
        print("[VISUALIZATION CACHE] HIT")
        logger.info("[VISUALIZATION CACHE] HIT")
        viz_result = {
            "image_url": existing,
            "status": "success",
            "source": "persisted",
        }
    else:
        print("[VISUALIZATION CACHE] MISS")
        if not ENABLE_AI_VISUALIZATION:
            print("[Visualization] AI generation disabled; technical floor plan only")
            _persist_visualization_status(state.workflow_id, "failed")
            state.design_result["ai_visualization"] = {
                "image_url": None, "status": "disabled", "source": "technical_floor_plan"
            }
            return state
        agent = VisualizationAgent(workflow_id=str(state.workflow_id))
        try:
            viz_result, _ = execute_once(
                state.workflow_id,
                "visualization",
                lambda: agent.process(state.design_result),
            )
        except DuplicateAIRequest:
            state.design_result["ai_visualization"] = {
                "image_url": None, "status": "pending", "source": "ai_guard"
            }
            return state
        if viz_result.get("status") == "success":
            try:
                image_url = _save_generated_image(viz_result)
                viz_result["image_url"] = image_url
                viz_result.pop("image_b64", None)
                _persist_visualization(state.workflow_id, image_url)
            except (OSError, ValueError, requests.RequestException) as exc:
                logger.error("[Visualization] Generated image could not be saved: %s", type(exc).__name__)
                _persist_visualization_status(state.workflow_id, "failed")
                viz_result = {
                    "image_url": None,
                    "status": "failed",
                    "error": "Generated image could not be persisted.",
                }
        else:
            _persist_visualization_status(state.workflow_id, "failed")
    state.design_result["ai_visualization"] = viz_result
    
    return state


def _headers() -> dict[str, str]:
    return {"X-Internal-API-Key": INTERNAL_API_KEY, "Content-Type": "application/json"}


def _save_generated_image(result: dict) -> str:
    """Materialize the temporary provider result and return its stable public URL."""
    print("[Visualization] Downloading generated image")
    encoded = result.get("image_b64")
    if encoded:
        try:
            image_bytes = base64.b64decode(encoded, validate=True)
        except (ValueError, TypeError) as exc:
            raise ValueError("Invalid base64 image returned by provider.") from exc
    else:
        temporary_url = result.get("image_url")
        if not temporary_url:
            raise ValueError("Generated image URL is missing.")
        response = requests.get(temporary_url, timeout=60)
        response.raise_for_status()
        image_bytes = response.content

    if not image_bytes:
        raise ValueError("Downloaded image was empty.")

    filename = f"{uuid.uuid4()}.png"
    VISUALIZATIONS_DIR.mkdir(parents=True, exist_ok=True)
    destination = VISUALIZATIONS_DIR / filename
    temporary = destination.with_suffix(".tmp")
    temporary.write_bytes(image_bytes)
    temporary.replace(destination)
    relative_path = f"storage/visualizations/{filename}"
    print(f"[Visualization] Saved image: {relative_path}")
    return f"{AGENTIC_PUBLIC_BASE_URL}/visualizations/{filename}"


def _existing_visualization(workflow_id) -> str | None:
    try:
        response = requests.get(
            f"{ASPNET_API_URL}/internal/workflows/{workflow_id}/visualization",
            headers=_headers(), timeout=10, verify=False)
        if response.ok:
            return response.json().get("imageUrl")
    except requests.RequestException as exc:
        logger.warning("[Visualization] Existing-image check failed: %s", exc)
    return None


def _persist_visualization(workflow_id, image_url: str) -> None:
    try:
        response = requests.patch(
            f"{ASPNET_API_URL}/internal/workflows/{workflow_id}/visualization",
            json={"imageUrl": image_url, "status": "completed"}, headers=_headers(), timeout=10, verify=False)
        if not response.ok:
            logger.warning("[Visualization] Image persistence failed with HTTP %s", response.status_code)
    except requests.RequestException as exc:
        logger.warning("[Visualization] Image persistence failed: %s", exc)


def _persist_visualization_status(workflow_id, status: str) -> None:
    try:
        response = requests.patch(
            f"{ASPNET_API_URL}/internal/workflows/{workflow_id}/visualization",
            json={"imageUrl": None, "status": status}, headers=_headers(), timeout=10, verify=False)
        if not response.ok:
            logger.warning("[Visualization] Status persistence failed with HTTP %s", response.status_code)
    except requests.RequestException as exc:
        logger.warning("[Visualization] Status persistence failed: %s", exc)
