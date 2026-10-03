"""
Tests for the Validation Agent's persistence call to ASP.NET.

Verifies that:
  - The complete ValidationResult (rules[], actual, expected, reason, summary) is
    sent to the /internal/workflows/{id}/validation endpoint.
  - No rules are dropped before ASP.NET receives them.
  - actual / expected values are preserved with their original types.
  - No real network calls are made (requests.patch is monkeypatched).
  - The python-side validation logic is NOT re-tested here; only the persistence path.
"""

from __future__ import annotations

import json
from unittest.mock import MagicMock, patch
from uuid import UUID, uuid4

import pytest

from app.schemas.validation_schemas import RuleValidationResult, ValidationResult
from app.schemas.workflow_state import WorkflowState
import app.validation.design_validation_service as _svc


# ---------------------------------------------------------------------------
# Override the autouse conftest fixture at module scope.
#
# The global conftest `offline_services` fixture monkeypatches
# `app.agents.design_agent._persist_failure`, which transitively imports
# psycopg2 (not installed in this environment). Our module only uses
# `app.validation.design_validation_service` which has no psycopg2 dependency,
# so we provide a lightweight module-level override here.
# ---------------------------------------------------------------------------
@pytest.fixture(autouse=True)
def offline_services(monkeypatch):  # noqa: PT004
    monkeypatch.setattr("app.config.OPENAI_API_KEY", "dummy-key-for-tests")
    monkeypatch.setenv("DATABASE_CONNECTION_STRING", "postgresql://dummy:dummy@localhost:5432/dummy")
    with patch("app.orchestration.workflow_router.persist_workflow_plan_state"):
        yield


# ---------------------------------------------------------------------------
# Helpers / factories
# ---------------------------------------------------------------------------

def _pass_result() -> ValidationResult:
    """Two-rule PASS result mirroring real agent output."""
    return ValidationResult(
        passed=True,
        rules=[
            RuleValidationResult(
                rule_name="coverage",
                passed=True,
                reason="Ground coverage is 51.40% (1400.0 sqft), within 65.00%.",
                actual="51.40% (1400.0 sqft)",
                expected="<= 65.00% (1772.0 sqft)",
            ),
            RuleValidationResult(
                rule_name="preferences",
                passed=True,
                reason="All preferences matched (Bedrooms: 3, Floors: 1).",
                actual="Bedrooms=3, Floors=1",
                expected="Bedrooms=3, Floors=1",
            ),
        ],
        errors=[],
        summary="Validation PASSED: Proposal satisfies all rules.",
        revision_reason=None,
    )


def _fail_result() -> ValidationResult:
    """One-rule FAIL result with budget rule failure."""
    return ValidationResult(
        passed=False,
        rules=[
            RuleValidationResult(
                rule_name="budget",
                passed=False,
                reason="Estimated cost LKR 6,000,000 exceeds budget LKR 4,500,000.",
                actual="LKR 6,000,000 (+33.3% vs budget)",
                expected="<= LKR 4,950,000 (Budget + 10%)",
            ),
        ],
        errors=["Estimated cost LKR 6,000,000 exceeds budget LKR 4,500,000."],
        summary="Validation FAILED: 1 rule(s) failed.",
        revision_reason="Estimated cost LKR 6,000,000 exceeds budget LKR 4,500,000.",
    )


def _make_state(workflow_id: UUID | None = None) -> WorkflowState:
    return WorkflowState(workflow_id=workflow_id or uuid4())


# ---------------------------------------------------------------------------
# Tests: persistence request sends full ValidationResult
# ---------------------------------------------------------------------------


def test_submit_validation_calls_correct_endpoint(monkeypatch):
    """_submit_validation_result hits /internal/workflows/{id}/validation."""
    mock_patch = MagicMock(return_value=MagicMock(status_code=200))
    monkeypatch.setattr(_svc.requests, "patch", mock_patch)

    state = _make_state()
    _svc._submit_validation_result(state, _pass_result())

    mock_patch.assert_called_once()
    url: str = mock_patch.call_args.args[0]
    assert str(state.workflow_id) in url
    assert url.endswith("/validation")


def test_submit_sends_full_rules_array_not_stripped(monkeypatch):
    """The `rules` list with both entries must be present in the POST body."""
    captured: list[dict] = []

    def fake_patch(url, **kwargs):
        captured.append(kwargs.get("json", {}))
        return MagicMock(status_code=200)

    monkeypatch.setattr(_svc.requests, "patch", fake_patch)

    state = _make_state()
    _svc._submit_validation_result(state, _pass_result())

    assert len(captured) == 1
    body = captured[0]

    # Overall result preserved
    assert body["passed"] is True

    # rules[] not dropped
    assert "rules" in body
    assert isinstance(body["rules"], list)
    assert len(body["rules"]) == 2  # both rules sent

    rule_names = {r["rule_name"] for r in body["rules"]}
    assert "coverage" in rule_names
    assert "preferences" in rule_names


def test_submit_preserves_actual_and_expected_per_rule(monkeypatch):
    """actual and expected values are sent as-is for each rule."""
    captured: list[dict] = []

    def fake_patch(url, **kwargs):
        captured.append(kwargs.get("json", {}))
        return MagicMock(status_code=200)

    monkeypatch.setattr(_svc.requests, "patch", fake_patch)

    _svc._submit_validation_result(_make_state(), _pass_result())

    body = captured[0]
    coverage_rule = next(r for r in body["rules"] if r["rule_name"] == "coverage")
    assert "51.40%" in str(coverage_rule["actual"])
    assert "65.00%" in str(coverage_rule["expected"])
    assert coverage_rule["passed"] is True


def test_submit_failed_result_sends_full_evidence(monkeypatch):
    """A FAIL result must include failed rule evidence (actual, expected, reason)."""
    captured: list[dict] = []

    def fake_patch(url, **kwargs):
        captured.append(kwargs.get("json", {}))
        return MagicMock(status_code=200)

    monkeypatch.setattr(_svc.requests, "patch", fake_patch)

    _svc._submit_validation_result(_make_state(), _fail_result())

    body = captured[0]
    assert body["passed"] is False
    assert len(body["rules"]) == 1

    rule = body["rules"][0]
    assert rule["rule_name"] == "budget"
    assert rule["passed"] is False
    assert "6,000,000" in str(rule["actual"])
    assert "4,950,000" in str(rule["expected"])
    assert "exceeds" in rule["reason"]

    # summary and revision_reason preserved
    assert "FAILED" in body["summary"]
    assert body["revision_reason"] is not None


def test_submit_preserves_summary_and_errors(monkeypatch):
    """summary and errors[] are included in the body."""
    captured: list[dict] = []

    def fake_patch(url, **kwargs):
        captured.append(kwargs.get("json", {}))
        return MagicMock(status_code=200)

    monkeypatch.setattr(_svc.requests, "patch", fake_patch)

    _svc._submit_validation_result(_make_state(), _pass_result())

    body = captured[0]
    assert "summary" in body
    assert "PASSED" in body["summary"]
    assert "errors" in body
    assert isinstance(body["errors"], list)


def test_submit_uses_model_dump_not_repr(monkeypatch):
    """The payload must be valid JSON-serialisable dict (from model_dump), not a repr string."""
    captured: list[dict] = []

    def fake_patch(url, **kwargs):
        captured.append(kwargs.get("json", {}))
        return MagicMock(status_code=200)

    monkeypatch.setattr(_svc.requests, "patch", fake_patch)

    _svc._submit_validation_result(_make_state(), _pass_result())

    body = captured[0]
    # Must be a plain dict (json-serialisable), not a string repr
    assert isinstance(body, dict)
    # Confirm it survives a round-trip through json.dumps / json.loads
    round_tripped = json.loads(json.dumps(body))
    assert round_tripped["passed"] is True


def test_submit_network_error_is_non_fatal(monkeypatch):
    """A RequestException from requests must not propagate — agent pipeline must continue."""
    import requests as req_mod
    monkeypatch.setattr(
        _svc.requests,
        "patch",
        MagicMock(side_effect=req_mod.ConnectionError("unreachable")),
    )
    # Must not raise
    _svc._submit_validation_result(_make_state(), _pass_result())


def test_submit_sends_internal_api_key_header(monkeypatch):
    """The call must include the X-Internal-API-Key authentication header."""
    captured_headers: list[dict] = []

    def fake_patch(url, **kwargs):
        captured_headers.append(kwargs.get("headers", {}))
        return MagicMock(status_code=200)

    monkeypatch.setattr(_svc.requests, "patch", fake_patch)

    _svc._submit_validation_result(_make_state(), _pass_result())

    assert len(captured_headers) == 1
    assert "X-Internal-API-Key" in captured_headers[0]
    assert captured_headers[0]["X-Internal-API-Key"]  # must be non-empty
