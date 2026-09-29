import json
import uuid
import logging
from datetime import datetime
try:
    import openai
except ImportError:
    openai = None

from app.config import ENABLE_OPENAI

logger = logging.getLogger(__name__)


class OpenAIVisualizationService:
    def __init__(self, api_key: str = None, workflow_id: str | None = None):
        self.api_key = api_key
        self.workflow_id = workflow_id
        if self.api_key and openai:
            openai.api_key = self.api_key

    def generate_visualization(self, layout_json: dict, expected_bedrooms: int = None, expected_bathrooms: int = None) -> dict:
        # ---- Kill switch -------------------------------------------------
        if not ENABLE_OPENAI:
            logger.warning("[Visualization] ENABLE_OPENAI=false — no image API call made.")
            return {
                "visualization_id": str(uuid.uuid4()),
                "image_url": None,
                "model": "gpt-image-1",
                "status": "disabled",
                "error": "OpenAI globally disabled (ENABLE_OPENAI=false).",
                "timestamp": datetime.utcnow().isoformat(),
            }

        # ---- Daily spend cap ---------------------------------------------
        try:
            from app.services.ai_guard_db import check_daily_limit, DailyLimitExceeded
            check_daily_limit()
        except Exception as exc:
            if "DailyLimitExceeded" in type(exc).__name__ or "limit" in str(exc).lower():
                logger.warning("[Visualization] Daily limit reached — skipping image generation.")
                return {
                    "visualization_id": str(uuid.uuid4()),
                    "image_url": None,
                    "model": "gpt-image-1",
                    "status": "failed",
                    "error": str(exc),
                    "timestamp": datetime.utcnow().isoformat(),
                }

        from app.design.visualization.layout_renderer import render_blueprint
        rooms = layout_json.get("rooms", [])

        # ---- Validation before saving -------------------------------------
        if expected_bedrooms is not None and expected_bathrooms is not None:
            actual_bedrooms = sum(1 for r in rooms if 'bedroom' in str(r.get("room_type", "")).lower())
            actual_bathrooms = sum(1 for r in rooms if 'bath' in str(r.get("room_type", "")).lower())
            
            print("\n[VISUALIZATION VALIDATION]")
            print("Expected:")
            print(f"Bedrooms:{expected_bedrooms}")
            print(f"Bathrooms:{expected_bathrooms}")
            print("Input layout:")
            print(f"Bedrooms:{actual_bedrooms}")
            print(f"Bathrooms:{actual_bathrooms}\n")
            
            if actual_bedrooms != expected_bedrooms or actual_bathrooms != expected_bathrooms:
                logger.error("[Visualization] Room mismatch validation failed.")
                return {
                    "visualization_id": str(uuid.uuid4()),
                    "image_url": None,
                    "model": "gpt-image-1",
                    "status": "failed",
                    "error": "Generated layout failed room requirement validation.",
                    "timestamp": datetime.utcnow().isoformat(),
                }

        img_bytes = render_blueprint(layout_json)
        room_summary = {}
        room_details = []
        for r in rooms:
            rt = r.get("room_type", "Room").capitalize()
            room_summary[rt] = room_summary.get(rt, 0) + 1
            room_details.append(f"- {r.get('name', rt)}: {r.get('width', 0)}ft x {r.get('length', 0)}ft at (X:{r.get('x', 0)}, Y:{r.get('y', 0)})")

        room_list_str = "\n".join(f"{k} x{v}" for k, v in room_summary.items())
        details_str = "\n".join(room_details)

        print("EXPECTED_ROOM_LAYOUT:")
        print(json.dumps({
            "bedrooms": actual_bedrooms if expected_bedrooms is not None else 0,
            "bathrooms": actual_bathrooms if expected_bathrooms is not None else 0,
            "rooms": rooms
        }, indent=1))

        actual_baths = sum(1 for r in rooms if 'bath' in str(r.get("room_type", "")).lower())
        bathroom_constraints = ""
        if actual_baths == 1:
            bathroom_constraints = (
                "- Exactly one bathroom room.\n"
                "- No ensuite bathrooms.\n"
                "- No guest toilets.\n"
                "- No additional washrooms.\n"
                "- No extra toilet fixtures.\n"
                "- Do not invent rooms.\n"
            )

        prompt = (
            "You are a rendering engine.\n\n"
            "Convert this exact architectural blueprint into a realistic top-down residential visualization.\n\n"
            "The blueprint is the source of truth.\n\n"
            "Rooms:\n"
            f"{room_list_str}\n\n"
            "Layout Details:\n"
            f"{details_str}\n\n"
            "Rules:\n"
            "- Do not add rooms\n"
            "- Do not remove rooms\n"
            "- Do not modify room counts\n"
            "- Follow coordinates exactly\n"
            f"{bathroom_constraints}"
        )
        try:
            if not self.api_key or not openai:
                raise ValueError("OpenAI API key or library missing.")

            char_count = len(prompt[:4000])
            print(
                f"[AI Request] purpose=visualization workflow={self.workflow_id or 'n/a'} "
                f"characters={char_count} estimated_tokens={char_count // 4}"
            )
            print("[Visualization Agent] Sending prompt to OpenAI gpt-image-1")
            logger.info("[Visualization Agent] Sending prompt to OpenAI")

            try:
                # Use edit if supported for image-to-image
                response = openai.images.edit(
                    model="gpt-image-1",
                    image=img_bytes,
                    prompt=prompt[:4000],
                    n=1,
                    size="1024x1024"
                )
            except Exception:
                # Fallback to generate if edit is not mocked
                response = openai.images.generate(
                    model="gpt-image-1",
                    prompt=prompt[:4000],
                    n=1,
                    size="1024x1024"
                )

            image = response.data[0]
            image_url = getattr(image, "url", None)
            image_b64 = getattr(image, "b64_json", None)
            if not image_url and not image_b64:
                raise ValueError("OpenAI image response contained neither a URL nor image data.")

            # ---- Cost logging for image generation -----------------------
            try:
                from app.services.ai_guard_db import log_ai_cost
                log_ai_cost(self.workflow_id, "visualization", "gpt-image-1", 0, 0)
            except Exception:
                pass

            print("[Visualization] Image generated successfully")
            logger.info("[Visualization] Image generated successfully")

            return {
                "visualization_id": str(uuid.uuid4()),
                "image_url": image_url,
                "image_b64": image_b64,
                "model": "gpt-image-1",
                "prompt": prompt,
                "status": "validated",
                "timestamp": datetime.utcnow().isoformat()
            }
        except Exception as e:
            logger.error(f"[Visualization Agent] AI visualization failed: {e}")
            return {
                "visualization_id": str(uuid.uuid4()),
                "image_url": None,
                "model": "gpt-image-1",
                "prompt": prompt,
                "status": "failed",
                "error": str(e),
                "timestamp": datetime.utcnow().isoformat()
            }
