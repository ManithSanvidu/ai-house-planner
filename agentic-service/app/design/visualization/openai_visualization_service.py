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

    def generate_visualization(self, layout_json: dict) -> dict:
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

        # ---- Build prompt (geometry untouched) ---------------------------
        rooms = layout_json.get("rooms", [])
        if rooms:
            min_x = min(r.get("x", 0) for r in rooms)
            max_x = max(r.get("x", 0) + r.get("width", 0) for r in rooms)
            min_y = min(r.get("y", 0) for r in rooms)
            max_y = max(r.get("y", 0) + r.get("length", r.get("height", 0)) for r in rooms)
            center_x = (min_x + max_x) / 2
            center_y = (min_y + max_y) / 2
        else:
            center_x = center_y = 0

        descriptions = []
        for r in rooms:
            name = r.get("name") or r.get("room_type", "Room").replace("_", " ").title()
            rx = r.get("x", 0)
            ry = r.get("y", 0)

            ns = "north" if ry > center_y else "south"
            ew = "east" if rx > center_x else "west"

            if abs(ry - center_y) < (max_y - min_y) * 0.2:
                ns = ""
            if abs(rx - center_x) < (max_x - min_x) * 0.2:
                ew = ""

            if not ns and not ew:
                loc = "in the center"
            elif not ns:
                loc = f"{ew} side"
            elif not ew:
                loc = f"{ns} side"
            else:
                loc = f"at {ns}-{ew}"

            descriptions.append(f"{name} is located {loc}.")

        layout_desc = "\n".join(descriptions)

        prompt = (
            "Create a realistic architectural visualization based on this exact floor arrangement.\n\n"
            f"{layout_desc}\n\n"
            "Do not change room positions.\n"
            "Do not add/remove rooms.\n"
            "Generate a professional architectural floor visualization."
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
                "status": "success",
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
