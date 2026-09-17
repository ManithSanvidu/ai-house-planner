import json
import re
from typing import List, Optional
from pydantic import BaseModel

ALLOWED_TERRAINS = {"flat", "hillside", "coastal"}
ALLOWED_SLOPES = {"flat", "gentle", "moderate", "steep", "unknown"}

class TerrainResult(BaseModel):
    terrain_type: str
    slope_estimate: str
    notable_features: List[str]

def _parse_terrain_result(raw_text: str) -> Optional[TerrainResult]:
    """Parse and validate JSON response for terrain classification."""
    if not raw_text or not isinstance(raw_text, str):
        return None
    try:
        # Strip markdown code fences if present (e.g. ```json ... ```)
        text = raw_text.strip()
        if text.startswith("```"):
            text = re.sub(r"^```(?:json)?\s*", "", text)
            text = re.sub(r"\s*```$", "", text)
        text = text.strip()

        data = json.loads(text)
        if not isinstance(data, dict):
            return None

        terrain_type = str(data.get("terrain_type", "")).lower()
        slope_estimate = str(data.get("slope_estimate", "unknown")).lower()

        if terrain_type not in ALLOWED_TERRAINS:
            return None
        if slope_estimate not in ALLOWED_SLOPES:
            return None

        notable_features = data.get("notable_features", [])
        if not isinstance(notable_features, list):
            notable_features = []

        return TerrainResult(
            terrain_type=terrain_type,
            slope_estimate=slope_estimate,
            notable_features=[str(f) for f in notable_features]
        )
    except Exception:
        return None

def _safe_fallback(reason: str) -> TerrainResult:
    """Fallback classification when vision processing fails or is indeterminate."""
    return TerrainResult(
        terrain_type="flat",
        slope_estimate="unknown",
        notable_features=[f"fallback:{reason}"]
    )

def vision_classify_tool(photo_url: str) -> TerrainResult:
    """Vision classify tool for terrain analysis."""
    if not photo_url:
        return _safe_fallback("no_photo_url")
    return TerrainResult(
        terrain_type="flat",
        slope_estimate="unknown",
        notable_features=["mocked_vision_result"]
    )
