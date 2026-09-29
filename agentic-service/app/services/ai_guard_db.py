"""
Persistent AI execution guard backed by PostgreSQL.

Replaces the in-memory ai_guard.py for durable, cross-restart deduplication.

Tables managed here:
  - AIExecutionGuard     -- one row per (workflow_id, purpose); tracks running/completed
  - OpenAISpendingLog    -- one row per billable call; daily limit enforcement

Public API (mirrors ai_guard.py):
  execute_once_db(workflow_id, purpose, operation)  -> (result, was_cached)
  log_ai_cost(workflow_id, purpose, model, input_tokens, output_tokens)
  check_daily_limit()  -> raises DailyLimitExceeded if over budget
  reset_ai_guard_db(workflow_id)  -> test helper only
"""
from __future__ import annotations

import json
import logging
from copy import deepcopy
from typing import Callable, TypeVar

import psycopg2
import psycopg2.extras

from app.config import get_db_connection_string, OPENAI_DAILY_LIMIT_USD

logger = logging.getLogger(__name__)
T = TypeVar("T")


# ---------------------------------------------------------------------------
# Exceptions
# ---------------------------------------------------------------------------

class DuplicateAIRequest(RuntimeError):
    """Raised when an AI call is already in progress or completed for this key."""

class DailyLimitExceeded(RuntimeError):
    """Raised when the daily OpenAI spending cap has been reached."""

class OpenAIDisabled(RuntimeError):
    """Raised when ENABLE_OPENAI=false kills all OpenAI requests."""


# ---------------------------------------------------------------------------
# DB helpers
# ---------------------------------------------------------------------------

# Process-level lock prevents thread races within a single uvicorn worker.
from threading import Lock
_lock = Lock()

_dsn: str | None = None

def _get_dsn() -> str:
    global _dsn
    if _dsn is None:
        _dsn = get_db_connection_string()
    return _dsn

def _connect():
    return psycopg2.connect(_get_dsn())


def _ensure_tables() -> None:
    """Idempotently create guard and spending tables."""
    ddl = """
    CREATE TABLE IF NOT EXISTS "AIExecutionGuard" (
        "Id"          SERIAL PRIMARY KEY,
        "WorkflowId"  TEXT        NOT NULL,
        "Purpose"     TEXT        NOT NULL,
        "Status"      TEXT        NOT NULL DEFAULT 'running',
        "ResultJson"  TEXT,
        "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        "UpdatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        CONSTRAINT uq_guard_workflow_purpose UNIQUE ("WorkflowId", "Purpose")
    );

    CREATE TABLE IF NOT EXISTS "OpenAISpendingLog" (
        "Id"            SERIAL PRIMARY KEY,
        "WorkflowId"    TEXT,
        "Purpose"       TEXT        NOT NULL,
        "Model"         TEXT        NOT NULL,
        "InputTokens"   INTEGER     NOT NULL DEFAULT 0,
        "OutputTokens"  INTEGER     NOT NULL DEFAULT 0,
        "EstimatedUsd"  NUMERIC(12,6) NOT NULL DEFAULT 0,
        "CalledAt"      TIMESTAMPTZ NOT NULL DEFAULT NOW()
    );
    """
    try:
        conn = _connect()
        with conn:
            with conn.cursor() as cur:
                cur.execute(ddl)
        conn.close()
    except Exception as exc:
        logger.error("[AIGuardDB] Table init failed: %s", exc)
        raise

try:
    _ensure_tables()
except Exception:
    logger.warning(
        "[AIGuardDB] Could not initialise tables -- guard will fall back to "
        "in-memory mode when DB is unavailable."
    )


# ---------------------------------------------------------------------------
# Cost model
# ---------------------------------------------------------------------------

# USD per 1M tokens (input_rate, output_rate)
_COST_TABLE: dict[str, tuple[float, float]] = {
    "gpt-4o":                  (2.50, 10.00),
    "gpt-4o-mini":             (0.15,  0.60),
    "gpt-4-turbo":             (10.00, 30.00),
    "gpt-image-1":             (0.00,   0.00),   # flat rate below
    "text-embedding-3-small":  (0.02,   0.00),
    "text-embedding-3-large":  (0.13,   0.00),
}

_IMAGE_FLAT_USD: dict[str, float] = {
    "gpt-image-1": 0.040,
    "dall-e-3":    0.040,
}


def _estimate_cost(model: str, input_tokens: int, output_tokens: int) -> float:
    m = model.lower()
    if m in _IMAGE_FLAT_USD:
        return _IMAGE_FLAT_USD[m]
    for key, (in_rate, out_rate) in _COST_TABLE.items():
        if key in m:
            return (input_tokens * in_rate + output_tokens * out_rate) / 1_000_000
    # Unknown -- estimate at gpt-4o rates
    return (input_tokens * 2.50 + output_tokens * 10.00) / 1_000_000


# ---------------------------------------------------------------------------
# Public: cost logging
# ---------------------------------------------------------------------------

def log_ai_cost(
    workflow_id: str | None,
    purpose: str,
    model: str,
    input_tokens: int = 0,
    output_tokens: int = 0,
) -> float:
    """Persist a spending record and emit [AI COST] log line. Returns USD cost."""
    usd = _estimate_cost(model, input_tokens, output_tokens)
    print(
        f"[AI COST] workflow={workflow_id or 'n/a'} purpose={purpose} "
        f"model={model} input_tokens={input_tokens} output_tokens={output_tokens} "
        f"cost_usd={usd:.6f}"
    )
    logger.info(
        "[AI COST] workflow=%s purpose=%s model=%s input=%d output=%d cost_usd=%.6f",
        workflow_id, purpose, model, input_tokens, output_tokens, usd,
    )
    try:
        conn = _connect()
        with conn:
            with conn.cursor() as cur:
                cur.execute(
                    'INSERT INTO "OpenAISpendingLog" '
                    '("WorkflowId","Purpose","Model","InputTokens","OutputTokens","EstimatedUsd") '
                    "VALUES (%s,%s,%s,%s,%s,%s)",
                    (str(workflow_id) if workflow_id else None,
                     purpose, model, input_tokens, output_tokens, usd),
                )
        conn.close()
    except Exception as exc:
        logger.warning("[AIGuardDB] Could not persist spending log: %s", exc)
    return usd


# ---------------------------------------------------------------------------
# Public: daily limit check
# ---------------------------------------------------------------------------

def check_daily_limit() -> None:
    """Raise DailyLimitExceeded if today's accumulated spend >= OPENAI_DAILY_LIMIT_USD."""
    try:
        conn = _connect()
        with conn.cursor() as cur:
            cur.execute(
                'SELECT COALESCE(SUM("EstimatedUsd"), 0) '
                'FROM "OpenAISpendingLog" '
                'WHERE "CalledAt"::date = CURRENT_DATE',
            )
            row = cur.fetchone()
        conn.close()
        today_usd = float(row[0]) if row else 0.0
        if today_usd >= OPENAI_DAILY_LIMIT_USD:
            msg = (
                f"[AI COST] Daily limit reached: ${today_usd:.4f} >= "
                f"${OPENAI_DAILY_LIMIT_USD:.2f}. Blocking OpenAI call."
            )
            print(msg)
            logger.warning(msg)
            raise DailyLimitExceeded(msg)
    except DailyLimitExceeded:
        raise
    except Exception as exc:
        logger.warning("[AIGuardDB] Could not check daily limit (non-fatal): %s", exc)


# ---------------------------------------------------------------------------
# Public: persistent execute_once
# ---------------------------------------------------------------------------

def execute_once_db(
    workflow_id: object,
    purpose: str,
    operation: Callable[[], T],
) -> tuple[T, bool]:
    """
    Execute one billable AI call per (workflow_id, purpose) pair, persistently.

    Returns (result, was_cached).
    Raises DuplicateAIRequest if the call is currently running.
    Raises DailyLimitExceeded if spending cap is reached.
    Falls back to unguarded execution if DB is unavailable.
    """
    wid = str(workflow_id)
    try:
        return _execute_with_db(wid, purpose, operation)
    except (DuplicateAIRequest, DailyLimitExceeded):
        raise
    except Exception as exc:
        logger.warning("[AIGuardDB] DB guard unavailable (%s); failing closed.", exc)
        return {"status": "blocked", "reason": "AI protection unavailable"}, False

import hashlib

def create_ai_execution_key(project_id: str, purpose: str, payload: dict) -> str:
    """Generate a stable cache key based on the payload."""
    payload_hash = hashlib.sha256(
        json.dumps(payload, sort_keys=True).encode("utf-8")
    ).hexdigest()[:8]
    return f"{project_id}:{purpose}:{payload_hash}"


def _execute_with_db(wid: str, purpose: str, operation: Callable[[], T]) -> tuple[T, bool]:
    with _lock:
        existing = _fetch_guard_row(wid, purpose)

        if existing:
            status = existing["status"]
            if status == "completed" and existing.get("result_json"):
                print(f"[AI GUARD DB] CACHE HIT workflow={wid} purpose={purpose}")
                logger.info("[AI GUARD DB] CACHE HIT workflow=%s purpose=%s", wid, purpose)
                return json.loads(existing["result_json"]), True
            if status == "running":
                print(f"[AI GUARD DB] BLOCKED (running) workflow={wid} purpose={purpose}")
                raise DuplicateAIRequest(
                    f"AI call in progress for workflow={wid} purpose={purpose}"
                )
            # status == "failed" -- allow retry
            _update_guard_status(wid, purpose, "running")
        else:
            _insert_guard_row(wid, purpose)

    # Outside lock: run the actual OpenAI call
    try:
        result = operation()
    except Exception:
        _update_guard_status(wid, purpose, "failed")
        raise

    result_json = json.dumps(deepcopy(result), default=str)
    with _lock:
        _update_guard_completed(wid, purpose, result_json)

    return result, False


# ---------------------------------------------------------------------------
# Internal DB operations
# ---------------------------------------------------------------------------

def _fetch_guard_row(wid: str, purpose: str) -> dict | None:
    conn = _connect()
    try:
        with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
            cur.execute(
                'SELECT "Status" as status, "ResultJson" as result_json '
                'FROM "AIExecutionGuard" '
                'WHERE "WorkflowId"=%s AND "Purpose"=%s LIMIT 1',
                (wid, purpose),
            )
            row = cur.fetchone()
        return dict(row) if row else None
    finally:
        conn.close()


def _insert_guard_row(wid: str, purpose: str) -> None:
    conn = _connect()
    with conn:
        with conn.cursor() as cur:
            cur.execute(
                'INSERT INTO "AIExecutionGuard" ("WorkflowId","Purpose","Status") '
                "VALUES (%s,%s,'running') "
                'ON CONFLICT ("WorkflowId","Purpose") DO NOTHING',
                (wid, purpose),
            )
    conn.close()


def _update_guard_status(wid: str, purpose: str, status: str) -> None:
    conn = _connect()
    with conn:
        with conn.cursor() as cur:
            cur.execute(
                'UPDATE "AIExecutionGuard" SET "Status"=%s, "UpdatedAt"=NOW() '
                'WHERE "WorkflowId"=%s AND "Purpose"=%s',
                (status, wid, purpose),
            )
    conn.close()


def _update_guard_completed(wid: str, purpose: str, result_json: str) -> None:
    conn = _connect()
    with conn:
        with conn.cursor() as cur:
            cur.execute(
                'UPDATE "AIExecutionGuard" '
                'SET "Status"=\'completed\', "ResultJson"=%s, "UpdatedAt"=NOW() '
                'WHERE "WorkflowId"=%s AND "Purpose"=%s',
                (result_json, wid, purpose),
            )
    conn.close()


# ---------------------------------------------------------------------------
# Test helpers
# ---------------------------------------------------------------------------

def reset_ai_guard_db(workflow_id: str | None = None) -> None:
    """Delete guard rows. Test helper only -- never call from production code."""
    conn = _connect()
    with conn:
        with conn.cursor() as cur:
            if workflow_id:
                cur.execute(
                    'DELETE FROM "AIExecutionGuard" WHERE "WorkflowId"=%s',
                    (workflow_id,),
                )
            else:
                cur.execute('DELETE FROM "AIExecutionGuard"')
    conn.close()


def get_today_spend_usd() -> float:
    """Return today's accumulated spend in USD. Used in tests."""
    try:
        conn = _connect()
        with conn.cursor() as cur:
            cur.execute(
                'SELECT COALESCE(SUM("EstimatedUsd"), 0) '
                'FROM "OpenAISpendingLog" '
                'WHERE "CalledAt"::date = CURRENT_DATE'
            )
            row = cur.fetchone()
        conn.close()
        return float(row[0]) if row else 0.0
    except Exception:
        return 0.0
