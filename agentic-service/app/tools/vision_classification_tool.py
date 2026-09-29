"""
Vision classification tool for land terrain analysis.

Uses Google Gemini Vision API to analyze a land photograph and return
structured terrain classification. Falls back safely on failure.

System prompt enforces JSON-only output with controlled enum values.
"""
import json

from app.config import OPENAI_API_KEY, ENABLE_OPENAI
from app.schemas.terrain_result import TerrainResult

# The strict system prompt — forces JSON-only output with controlled values
LAND_ANALYSIS_SYSTEM_PROMPT = """You are the Land Analysis Agent in an AI-assisted home design planning system.

Your ONLY responsibility is to classify terrain from a land photograph.

You must return ONLY valid JSON. No markdown, no explanations, no code blocks.

Allowed terrain_type values:
- flat
- hillside
- coastal
- unknown (insufficient evidence)

Allowed slope_estimate values:
- flat
- gentle
- moderate
- steep
- unknown

Return exactly this JSON structure:
{"terrain_type": "...", "slope_estimate": "...", "notable_features": []}

Rules:
- Do not design a house.
- Do not estimate cost.
- Do not provide architectural advice.
- Do not return markdown.
- If the image does not provide enough evidence, use "slope_estimate": "unknown".
- Never invent unsupported observations.
- notable_features should be short lowercase identifiers like "tree_cover_north", "retaining_wall"."""

# Even stricter prompt for retry
RETRY_PROMPT = """Return ONLY a JSON object. No text before or after.
{"terrain_type": "Union[flat, hillside]|Union[coastal, unknown]", "slope_estimate": "Union[flat, gentle]|Union[moderate, steep]|unknown", "notable_features": []}"""



def vision_classification_tool(photo_url: str, workflow_id: object | None = None) -> TerrainResult:
    # Kill switch
    if not ENABLE_OPENAI:
        print("[Vision Tool] ENABLE_OPENAI=false -- returning fallback, no API call made.")
        return _safe_fallback("openai_disabled")

    if not OPENAI_API_KEY:
        print("[Vision Tool] No OPENAI_API_KEY set. Returning manual terrain required.")
        return _safe_fallback("manual_terrain_required")

    # Daily spend cap
    try:
        from app.services.ai_guard_db import check_daily_limit, DailyLimitExceeded
        check_daily_limit()
    except Exception as exc:
        if "DailyLimitExceeded" in type(exc).__name__ or "limit" in str(exc).lower():
            print(f"[Vision Tool] Daily limit reached -- skipping vision call.")
            return _safe_fallback("daily_limit_exceeded")

    try:
        if workflow_id is not None:
            from app.services.ai_guard import execute_once
            result_text, _ = execute_once(
                workflow_id, "terrain_vision",
                lambda: _call_openai_vision(photo_url, LAND_ANALYSIS_SYSTEM_PROMPT, workflow_id),
            )
        else:
            result_text = _call_openai_vision(photo_url, LAND_ANALYSIS_SYSTEM_PROMPT, workflow_id)
        terrain = _parse_terrain_result(result_text)
        if terrain:
            return terrain
        return _safe_fallback("vision_parse_failed")
    except Exception as e:
        print(f"[Vision Tool] Vision API error: {e}")
        return _safe_fallback(f"api_error: {str(e)[:100]}")

def _call_openai_vision(photo_url: str, prompt: str, workflow_id: object | None = None) -> str:
    """Call OpenAI Vision API with an image URL and return the raw text response."""
    import requests
    characters = len(prompt) + len(photo_url)
    print(
        f"[AI Request] purpose=terrain_vision workflow={workflow_id or 'n/a'} "
        f"characters={characters} estimated_tokens={characters // 4}"
    )
    response = requests.post(
        "https://api.openai.com/v1/chat/completions",
        headers={
            "Authorization": f"Bearer {OPENAI_API_KEY}",
            "Content-Type": "application/json"
        },
        json={
            "model": "gpt-4o",
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {"type": "text", "text": prompt},
                        {"type": "image_url", "image_url": {"url": photo_url}}
                    ]
                }
            ],
            "max_tokens": 256,
            "temperature": 0.1
        },
        timeout=120
    )
    response.raise_for_status()
    data = response.json()
    # Log cost
    usage = data.get("usage", {})
    try:
        from app.services.ai_guard_db import log_ai_cost
        log_ai_cost(
            str(workflow_id) if workflow_id else None,
            "terrain_vision", "gpt-4o",
            usage.get("prompt_tokens", 0),
            usage.get("completion_tokens", 0),
        )
    except Exception:
        pass
    print(
        f"[Model Usage] provider=openai purpose=terrain_vision workflow={workflow_id or 'n/a'} "
        f"input_tokens={usage.get('prompt_tokens', '?')} "
        f"output_tokens={usage.get('completion_tokens', '?')}"
    )
    if 'choices' in data and len(data['choices']) > 0:
        return data['choices'][0]['message']['content'].strip()
    return "{}"



def _parse_terrain_result(text: str) -> TerrainResult | None:
    """
    Parse raw LLM text into a validated TerrainResult.
    Handles common issues like markdown code blocks around JSON.
    Invalid terrain or slope values fail closed instead of being silently accepted.
    """
    # Strip markdown code block wrappers if present
    cleaned = text.strip()
    if cleaned.startswith("```"):
        lines = cleaned.split("\n")
        cleaned = "\n".join(
            line for line in lines
            if not line.strip().startswith("```")
        ).strip()

    try:
        data = json.loads(cleaned)
        if not isinstance(data, dict):
            return None
        if data.get("terrain_type") not in {"flat", "hillside", "coastal", "unknown"}:
            print(f"[Vision Tool] Invalid terrain type rejected: {data.get('terrain_type')}")
            return None
        if data.get("slope_estimate") not in {"flat", "gentle", "moderate", "steep", "unknown"}:
            print(f"[Vision Tool] Invalid slope rejected: {data.get('slope_estimate')}")
            return None
        return TerrainResult(**data)
    except (json.JSONDecodeError, ValueError, TypeError) as e:
        print(f"[Vision Tool] Parse error: {e}")
        return None


def _safe_fallback(reason: str) -> TerrainResult:
    """
    Return a safe fallback result when vision analysis fails.
    Uses conservative values and flags the failure.
    """
    print(f"[Vision Tool] Using safe fallback. Reason: {reason}")
    return TerrainResult(
        terrain_type="unknown",
        slope_estimate="unknown",
        notable_features=["mocked_vision_result"]
    )
