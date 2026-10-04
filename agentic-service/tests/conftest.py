"""Unit tests never call paid APIs or write workflow state to a local server."""

import os
import pytest
from unittest.mock import patch

# Set the database before test modules import app.config or ai_guard_db. Local
# developer credentials must never be used by the test process.
os.environ["DATABASE_CONNECTION_STRING"] = os.environ.get(
    "TEST_DATABASE_CONNECTION_STRING",
    "Host=127.0.0.1;Port=5432;Database=agentic_test;Username=postgres;Password=password;",
)
os.environ.setdefault("INTERNAL_API_KEY", "test-only-internal-api-key")


@pytest.fixture(autouse=True)
def mock_plan_persistence(request):
    if request.module.__name__ == "tests.test_plan_persistence":
        yield None
        return
    with patch("app.orchestration.workflow_router.persist_workflow_plan_state") as m:
        yield m


@pytest.fixture(autouse=True)
def offline_services(monkeypatch):
    monkeypatch.setattr('app.config.OPENAI_API_KEY', 'dummy-key-for-tests')
    monkeypatch.setenv('DATABASE_CONNECTION_STRING', 'postgresql://dummy:dummy@localhost:5432/dummy')
    monkeypatch.setattr('app.agents.land_analysis_agent._persist_terrain', lambda state: None)
    monkeypatch.setattr('app.agents.design_agent._persist_failure', lambda state: None)
    monkeypatch.setattr('app.orchestration.tool_audit.persist_tool_audit_log', lambda *_args: True)
    monkeypatch.setattr('app.orchestration.tool_governance.persist_tool_audit_log', lambda *_args: True)
