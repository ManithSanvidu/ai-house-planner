"""
OpenAI cost protection integration tests.

Test 1: Same workflow run twice -> one OpenAI call.
Test 2: Simulate restart -> zero duplicate calls (DB guard persists).
Test 3: Existing visualization in DB -> image API not called.
Test 4: ENABLE_OPENAI=false -> no HTTP request made.

All tests use monkeypatching and an in-memory DB stub so no real OpenAI
or PostgreSQL connection is required.
"""
from __future__ import annotations

import json
from unittest.mock import MagicMock, patch, call
import pytest


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_fake_db(rows: dict | None = None):
    """
    Returns a fake psycopg2 module whose connect() returns a context-managed
    connection backed by an in-memory dict keyed by (wid, purpose).
    """
    store: dict[tuple[str, str], dict] = rows or {}

    class FakeCursor:
        def __init__(self):
            self._last_row = None
            self.cursor_factory = None

        def __enter__(self): return self
        def __exit__(self, *a): pass

        def execute(self, sql, params=None):
            sql_strip = sql.strip()
            # SELECT for guard row
            if "SELECT" in sql_strip and "AIExecutionGuard" in sql_strip and params:
                wid, purpose = params
                row = store.get((wid, purpose))
                self._last_row = {"status": row["status"], "result_json": row.get("result_json")} if row else None
            # SELECT daily spend
            elif "SELECT" in sql_strip and "OpenAISpendingLog" in sql_strip:
                self._last_row = (0,)
            # INSERT guard
            elif "INSERT INTO" in sql_strip and "AIExecutionGuard" in sql_strip and params:
                wid, purpose = params[0], params[1]
                if (wid, purpose) not in store:
                    store[(wid, purpose)] = {"status": "running", "result_json": None}
            # UPDATE guard status
            elif "UPDATE" in sql_strip and "AIExecutionGuard" in sql_strip and params:
                status, wid, purpose = params
                if (wid, purpose) in store:
                    store[(wid, purpose)]["status"] = status
                    if status == "completed" and len(params) == 4:
                        store[(wid, purpose)]["result_json"] = params[1] if len(params) > 2 else None
            # INSERT spending
            elif "INSERT INTO" in sql_strip and "OpenAISpendingLog" in sql_strip:
                pass  # ignore in unit tests
            # CREATE TABLE — ignore
            elif "CREATE TABLE" in sql_strip:
                pass

        def fetchone(self):
            if isinstance(self._last_row, dict):
                # Return a RealDictRow-like object for guard rows
                class _Row(dict):
                    def __getitem__(self, key):
                        return super().__getitem__(key)
                return _Row(self._last_row) if self._last_row else None
            return self._last_row

    class FakeConn:
        def __enter__(self): return self
        def __exit__(self, *a): pass
        def close(self): pass
        def cursor(self, cursor_factory=None):
            c = FakeCursor()
            c.cursor_factory = cursor_factory
            return c

    fake_psycopg2 = MagicMock()
    fake_psycopg2.connect.return_value = FakeConn()
    fake_psycopg2.extras = MagicMock()
    fake_psycopg2.extras.RealDictCursor = None

    return fake_psycopg2, store


# ---------------------------------------------------------------------------
# Test 1: Same workflow run twice -> one OpenAI call
# ---------------------------------------------------------------------------

def test_same_workflow_two_calls_one_openai_call(monkeypatch):
    """
    When execute_once_db is called twice with the same (workflow_id, purpose),
    the operation callable must only be invoked once.
    The second call returns the stored result from the DB guard.
    """
    fake_psycopg2, store = _make_fake_db()

    monkeypatch.setattr("app.services.ai_guard_db.get_db_connection_string", lambda: "dummy")
    monkeypatch.setattr("app.services.ai_guard_db._connect", lambda: fake_psycopg2.connect())
    monkeypatch.setattr("app.services.ai_guard_db._dsn", "dummy")

    call_count = 0
    def expensive_operation():
        nonlocal call_count
        call_count += 1
        return {"answer": 42}

    from app.services.ai_guard_db import execute_once_db, reset_ai_guard_db

    wid = "workflow-test-001"
    purpose = "test_purpose"

    # Patch _ensure_tables to no-op since we have a fake DB
    with patch("app.services.ai_guard_db._ensure_tables"):
        result1, cached1 = execute_once_db(wid, purpose, expensive_operation)
        # Manually mark the guard row as completed with the result
        store[(wid, purpose)] = {"status": "completed", "result_json": json.dumps({"answer": 42})}
        result2, cached2 = execute_once_db(wid, purpose, expensive_operation)

    assert result1 == {"answer": 42}, "First call must return operation result"
    assert cached1 is False, "First call is not cached"
    assert result2 == {"answer": 42}, "Second call must return same result"
    assert cached2 is True, "Second call must be a cache hit"
    assert call_count == 1, f"Operation should be called exactly once, got {call_count}"


# ---------------------------------------------------------------------------
# Test 2: Restart simulation -> zero duplicate calls (DB guard persists)
# ---------------------------------------------------------------------------

def test_restart_no_duplicate_calls(monkeypatch):
    """
    Simulate a server restart by importing execute_once_db in a fresh context.
    If the DB guard row is already 'completed', the operation must not be called again.
    """
    # Pre-populate DB with completed guard row
    stored_result = {"rooms": [{"name": "bedroom"}], "cost": 99}
    _, store = _make_fake_db(rows={
        ("workflow-restart-999", "design_strategy"): {
            "status": "completed",
            "result_json": json.dumps(stored_result),
        }
    })

    call_count = 0
    def expensive_operation():
        nonlocal call_count
        call_count += 1
        return {"new_data": "should_not_appear"}

    from app.services.ai_guard_db import execute_once_db

    fake_psycopg2 = MagicMock()
    fake_conn = MagicMock()
    fake_cursor = MagicMock()

    # Simulate RealDictCursor returning the existing completed row
    fake_cursor.__enter__ = lambda s: s
    fake_cursor.__exit__ = MagicMock(return_value=False)
    row_dict = {"status": "completed", "result_json": json.dumps(stored_result)}
    fake_cursor.fetchone.return_value = row_dict
    fake_cursor.execute = MagicMock()
    fake_conn.cursor.return_value = fake_cursor
    fake_conn.close = MagicMock()
    fake_psycopg2.connect.return_value = fake_conn
    fake_psycopg2.extras.RealDictCursor = None

    with patch("app.services.ai_guard_db._connect", return_value=fake_conn), \
         patch("app.services.ai_guard_db._ensure_tables"):
        result, cached = execute_once_db("workflow-restart-999", "design_strategy", expensive_operation)

    assert cached is True, "Should return cached result from DB after simulated restart"
    assert call_count == 0, f"Operation must not be called after restart if already completed, got {call_count}"
    assert result == stored_result, "Should return the stored result"


# ---------------------------------------------------------------------------
# Test 3: Existing visualization -> image API not called
# ---------------------------------------------------------------------------

def test_existing_visualization_image_api_not_called(monkeypatch):
    """
    If HouseDesign.AIVisualizationImage exists in the .NET API,
    visualization_node must return it without calling openai.images.generate.
    """
    import requests

    # Simulate existing visualization endpoint returning a URL
    existing_url = "http://localhost:8001/visualizations/existing-house.png"

    def fake_get(url, **kwargs):
        resp = MagicMock()
        resp.ok = True
        resp.json.return_value = {"imageUrl": existing_url}
        return resp

    monkeypatch.setattr(requests, "get", fake_get)

    # Make sure openai.images.generate is never called
    import openai as openai_mod
    mock_generate = MagicMock(side_effect=AssertionError("openai.images.generate must NOT be called!"))

    with patch("app.design.visualization.openai_visualization_service.openai") as mock_openai:
        mock_openai.images.generate = mock_generate
        from app.design.visualization.visualization_agent import _existing_visualization
        result = _existing_visualization("workflow-123")

    assert result == existing_url, "Should return existing image URL"
    mock_generate.assert_not_called()


# ---------------------------------------------------------------------------
# Test 4: ENABLE_OPENAI=false -> no HTTP request made
# ---------------------------------------------------------------------------

def test_enable_openai_false_blocks_all_requests(monkeypatch):
    """
    When ENABLE_OPENAI=false, every OpenAI provider must raise an error
    and make zero HTTP requests.
    """
    import requests

    http_call_count = 0
    original_post = requests.post

    def spy_post(*args, **kwargs):
        nonlocal http_call_count
        http_call_count += 1
        raise AssertionError("HTTP request was made despite ENABLE_OPENAI=false!")

    monkeypatch.setattr(requests, "post", spy_post)

    # Patch ENABLE_OPENAI=false in all modules that check it
    monkeypatch.setattr("app.config.ENABLE_OPENAI", False)
    monkeypatch.setattr("app.providers.openai_provider.ENABLE_OPENAI", False)
    monkeypatch.setattr("app.tools.vision_classification_tool.ENABLE_OPENAI", False)
    monkeypatch.setattr("app.knowledge.embeddings.ENABLE_OPENAI", False)
    monkeypatch.setattr("app.design.visualization.openai_visualization_service.ENABLE_OPENAI", False)
    monkeypatch.setattr("app.workflows.procurement_graph.ENABLE_OPENAI", False)

    # --- Test: OpenAIProvider.generate_json ---
    from app.providers.openai_provider import OpenAIProvider
    from app.providers.base_provider import ProviderUnavailableError
    from pydantic import BaseModel

    class _Schema(BaseModel):
        x: int

    provider = OpenAIProvider()
    with pytest.raises(ProviderUnavailableError, match="globally disabled"):
        provider.generate_json("sys", "user", _Schema)

    assert http_call_count == 0, "HTTP requests must not be made when ENABLE_OPENAI=false"

    # --- Test: vision_classification_tool ---
    from app.tools.vision_classification_tool import vision_classification_tool
    result = vision_classification_tool("http://example.com/land.jpg", workflow_id="wf-999")
    # Should return a safe fallback, not raise, not make HTTP call
    assert result.terrain_type == "unknown", "Should return safe fallback"
    assert http_call_count == 0, "Vision tool must not make HTTP call when disabled"

    # --- Test: embeddings ---
    from app.knowledge.embeddings import generate_embedding
    with pytest.raises(RuntimeError, match="globally disabled"):
        generate_embedding("test text")

    assert http_call_count == 0, "Embeddings must not make HTTP call when disabled"

    # --- Test: visualization service ---
    from app.design.visualization.openai_visualization_service import OpenAIVisualizationService
    svc = OpenAIVisualizationService(api_key="sk-test", workflow_id="wf-vis-999")
    result = svc.generate_visualization({"rooms": []})
    assert result["status"] == "disabled", "Visualization must return disabled status"
    assert http_call_count == 0, "Visualization must not make HTTP call when disabled"

    # --- Test: procurement graph _invoke_once ---
    from app.workflows.procurement_graph import _invoke_once, ProcurementState
    from app.providers.base_provider import ProviderUnavailableError as PUE
    from langchain_core.prompts import ChatPromptTemplate

    state: ProcurementState = {
        "project_id": "proj-test",
        "project_data": {},
        "construction_plan": [],
        "inventory": [],
        "current_phase": "",
        "upcoming_phases": [],
        "required_materials": [],
        "available_materials": [],
        "shortages": [],
        "procurement_plan": [],
        "procurement_priorities": [],
        "risks": [],
        "acceleration_opportunities": [],
        "final_recommendation": {},
    }
    prompt = ChatPromptTemplate.from_template("Test {value}")
    with pytest.raises(PUE, match="globally disabled"):
        _invoke_once(state, "test_purpose", prompt, {"value": "x"})

    assert http_call_count == 0, "No HTTP calls must be made when ENABLE_OPENAI=false"
