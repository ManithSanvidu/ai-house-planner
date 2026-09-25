"""Unit tests never call paid APIs or write workflow state to a local server."""
import os

import pytest

os.environ.setdefault("INTERNAL_API_KEY", "test-only-internal-api-key")


@pytest.fixture(autouse=True)
def offline_services(monkeypatch):
    monkeypatch.setattr('app.config.OPENAI_API_KEY', '')
    monkeypatch.setattr('app.agents.land_analysis_agent._persist_terrain', lambda state: None)
    monkeypatch.setattr('app.agents.design_agent._persist_failure', lambda state: None)
