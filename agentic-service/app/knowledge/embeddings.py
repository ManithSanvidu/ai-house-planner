from __future__ import annotations

import logging
import hashlib
import requests

from app.config import OPENAI_API_KEY, ENABLE_OPENAI
from app.services.ai_guard import execute_once

logger = logging.getLogger(__name__)

EMBEDDING_MODEL = "text-embedding-3-small"
EMBEDDING_DIM = 1536


def generate_embedding(text: str) -> list[float]:
    """Generate an embedding using OpenAI text-embedding-3-small."""
    if not ENABLE_OPENAI:
        raise RuntimeError("OpenAI is globally disabled (ENABLE_OPENAI=false). Cannot generate embedding.")
    if not OPENAI_API_KEY:
        raise RuntimeError("OPENAI_API_KEY is required for embedding generation.")

    try:
        from app.services.ai_guard_db import check_daily_limit, DailyLimitExceeded
        check_daily_limit()
    except Exception as exc:
        if "DailyLimitExceeded" in type(exc).__name__ or "limit" in str(exc).lower():
            raise RuntimeError(str(exc)) from exc

    key = hashlib.sha256(text.encode("utf-8")).hexdigest()
    def request() -> list[float]:
        print(f"[AI Request] purpose=embedding characters={len(text)} estimated_tokens={len(text) // 4}")
        resp = requests.post(
            "https://api.openai.com/v1/embeddings",
            headers={"Authorization": f"Bearer {OPENAI_API_KEY}", "Content-Type": "application/json"},
            json={"model": EMBEDDING_MODEL, "input": text}, timeout=30,
        )
        resp.raise_for_status()
        data = resp.json()
        usage = data.get("usage", {})
        try:
            from app.services.ai_guard_db import log_ai_cost
            log_ai_cost(None, "embedding", EMBEDDING_MODEL, usage.get("total_tokens", 0), 0)
        except Exception:
            pass
        return data["data"][0]["embedding"]
    result, _ = execute_once(key, "embedding", request)
    return result


def generate_embeddings_batch(texts: list[str]) -> list[list[float]]:
    """Generate embeddings for a batch of texts."""
    if not ENABLE_OPENAI:
        raise RuntimeError("OpenAI is globally disabled (ENABLE_OPENAI=false). Cannot generate embeddings.")
    if not OPENAI_API_KEY:
        raise RuntimeError("OPENAI_API_KEY is required for embedding generation.")

    try:
        from app.services.ai_guard_db import check_daily_limit, DailyLimitExceeded
        check_daily_limit()
    except Exception as exc:
        if "DailyLimitExceeded" in type(exc).__name__ or "limit" in str(exc).lower():
            raise RuntimeError(str(exc)) from exc

    serialized = "\n".join(texts)
    key = hashlib.sha256(serialized.encode("utf-8")).hexdigest()
    def request() -> list[list[float]]:
        print(f"[AI Request] purpose=embedding_batch characters={len(serialized)} estimated_tokens={len(serialized) // 4}")
        resp = requests.post(
            "https://api.openai.com/v1/embeddings",
            headers={"Authorization": f"Bearer {OPENAI_API_KEY}", "Content-Type": "application/json"},
            json={"model": EMBEDDING_MODEL, "input": texts}, timeout=30,
        )
        resp.raise_for_status()
        data = resp.json()
        usage = data.get("usage", {})
        try:
            from app.services.ai_guard_db import log_ai_cost
            log_ai_cost(None, "embedding_batch", EMBEDDING_MODEL, usage.get("total_tokens", 0), 0)
        except Exception:
            pass
        return [item["embedding"] for item in data["data"]]
    result, _ = execute_once(key, "embedding_batch", request)
    return result
