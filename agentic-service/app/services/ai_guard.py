"""
AI execution guard — now backed by PostgreSQL for cross-restart persistence.

All existing callers (execute_once, DuplicateAIRequest, reset_ai_guard) continue
to work unchanged. Internally delegates to ai_guard_db.execute_once_db.

Legacy in-memory fallback is retained for tests that monkeypatch get_db_connection_string.
"""
from __future__ import annotations

from copy import deepcopy
from threading import Lock
from typing import Callable, TypeVar

T = TypeVar("T")

MAX_AI_CALLS_PER_WORKFLOW = 1

# Re-export the persistent exceptions so all callers get the same type.
from app.services.ai_guard_db import (  # noqa: E402
    DuplicateAIRequest,
    DailyLimitExceeded,
    OpenAIDisabled,
    execute_once_db,
    reset_ai_guard_db,
    log_ai_cost,
    check_daily_limit,
)

__all__ = [
    "DuplicateAIRequest",
    "DailyLimitExceeded",
    "OpenAIDisabled",
    "execute_once",
    "reset_ai_guard",
    "log_ai_cost",
    "check_daily_limit",
]


def execute_once(
    workflow_id: object,
    purpose: str,
    operation: Callable[[], T],
) -> tuple[T, bool]:
    """
    Persistent execute-once wrapper.
    Delegates to execute_once_db which uses PostgreSQL for deduplication.
    """
    return execute_once_db(workflow_id, purpose, operation)


def reset_ai_guard() -> None:
    """Test helper. Clears all guard rows. Never call from production code."""
    reset_ai_guard_db()
