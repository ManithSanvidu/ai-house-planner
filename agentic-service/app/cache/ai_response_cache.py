from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
from typing import Any

from app.design.program.models import Requirements


class AIResponseCache:
    """File-backed cache for validated, JSON-serializable AI responses."""

    def __init__(self, directory: str | Path | None = None):
        configured = directory or os.getenv("AI_RESPONSE_CACHE_DIR")
        self.directory = Path(configured) if configured else Path(__file__).resolve().parents[2] / ".cache" / "ai-responses"

    def get(self, key: str) -> dict[str, Any] | None:
        path = self.directory / f"{key}.json"
        try:
            value = json.loads(path.read_text())
            return value if isinstance(value, dict) else None
        except (FileNotFoundError, json.JSONDecodeError, OSError):
            return None

    def set(self, key: str, value: dict[str, Any]) -> None:
        self.directory.mkdir(parents=True, exist_ok=True)
        path = self.directory / f"{key}.json"
        temporary = path.with_suffix(".tmp")
        temporary.write_text(json.dumps(value, sort_keys=True, separators=(",", ":")))
        temporary.replace(path)


def spatial_program_cache_key(req: Requirements, land_size_perches: float) -> str:
    requirements = req.model_dump(exclude={"design_seed"}, mode="json")
    payload = {
        "purpose": "design_strategy",
        "bedrooms": req.bedrooms,
        "bathrooms": req.bathrooms,
        "floors": req.floors,
        "style": req.style,
        "land_size": round(land_size_perches, 3),
        "requirements": requirements,
    }
    serialized = json.dumps(payload, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(serialized.encode("utf-8")).hexdigest()
