"""
Tests: Requirement Analysis Agent

A. No prompt → state unchanged
B. Natural language prompt → AI output merged correctly (mocked)
C. Explicit preferences override AI-inferred values
"""
import uuid
from unittest.mock import MagicMock, patch

from app.agents.requirement_analysis_agent import requirement_analysis_node
from app.schemas.workflow_state import CoordinatorInput, WorkflowState


def _make_state(
    preferences: dict | None = None,
    natural_language_prompt: str | None = None,
) -> WorkflowState:
    return WorkflowState(
        workflow_id=str(uuid.uuid4()),
        status="running",
        input_data=CoordinatorInput(
            submission_id=str(uuid.uuid4()),
            land_size_perches=10.0,
            preferences=preferences or {},
            natural_language_prompt=natural_language_prompt,
        ),
    )


# ---------------------------------------------------------------------------
# A. No prompt → state unchanged
# ---------------------------------------------------------------------------

def test_no_prompt_returns_state_unchanged():
    original_prefs = {"bedrooms": 4, "floors": 2}
    state = _make_state(preferences=original_prefs, natural_language_prompt=None)

    result = requirement_analysis_node(state)

    assert result.input_data.preferences == original_prefs, (
        "Preferences must not change when no prompt is provided"
    )
    # No execution log entry should be added (provider was never called)
    ra_logs = [e for e in result.execution_log if e.agent_name == "RequirementAnalysisAgent"]
    assert not ra_logs, "No log entry expected when prompt is absent"


def test_empty_string_prompt_returns_state_unchanged():
    state = _make_state(preferences={"bedrooms": 3}, natural_language_prompt="   ")
    result = requirement_analysis_node(state)
    assert result.input_data.preferences == {"bedrooms": 3}


# ---------------------------------------------------------------------------
# B. Natural language prompt → AI output merged correctly
# ---------------------------------------------------------------------------

_MOCK_AI_OUTPUT = {
    "bedrooms": 3,
    "bathrooms": 2,
    "floors": 1,
    "architecturalStyle": "modern minimalist",
    "homeOffice": True,
    "accessibility": None,      # Null fields must NOT be injected
    "separateDining": None,
    "parkingRequired": False,
    "masterEnsuite": None,
}


@patch(
    "app.agents.requirement_analysis_agent._provider.generate_json",
    return_value=_MOCK_AI_OUTPUT,
)
def test_prompt_merges_ai_output(mock_generate):
    state = _make_state(
        preferences={},
        natural_language_prompt="I want a modern single-storey house with a home office.",
    )

    result = requirement_analysis_node(state)
    prefs = result.input_data.preferences

    # Positively inferred fields must be present
    assert prefs["bedrooms"] == 3
    assert prefs["bathrooms"] == 2
    assert prefs["floors"] == 1
    assert prefs["style"] == "modern minimalist"
    assert prefs["home_office"] is True
    assert prefs["parking_required"] is False

    # Null AI fields must NOT be injected
    assert "accessibility" not in prefs
    assert "separate_dining" not in prefs
    assert "master_ensuite" not in prefs

    # Execution log entry must be recorded
    ra_logs = [e for e in result.execution_log if e.agent_name == "RequirementAnalysisAgent"]
    assert len(ra_logs) == 1
    assert "inferences applied" in ra_logs[0].action

    mock_generate.assert_called_once()


# ---------------------------------------------------------------------------
# C. Explicit preferences override AI-inferred values
# ---------------------------------------------------------------------------

@patch(
    "app.agents.requirement_analysis_agent._provider.generate_json",
    return_value=_MOCK_AI_OUTPUT,
)
def test_explicit_preferences_override_ai(mock_generate):
    # User explicitly set bedrooms=4 and floors=2 — AI says 3 and 1 respectively
    explicit = {"bedrooms": 4, "floors": 2}
    state = _make_state(
        preferences=explicit,
        natural_language_prompt="I want a cosy single-storey home with a study.",
    )

    result = requirement_analysis_node(state)
    prefs = result.input_data.preferences

    # Explicit values must win
    assert prefs["bedrooms"] == 4, "Explicit bedrooms=4 must override AI bedrooms=3"
    assert prefs["floors"] == 2, "Explicit floors=2 must override AI floors=1"

    # AI-only inferences should still be applied
    assert prefs["style"] == "modern minimalist"
    assert prefs["home_office"] is True

    mock_generate.assert_called_once()


# ---------------------------------------------------------------------------
# D. Provider error → graceful degradation, state unchanged
# ---------------------------------------------------------------------------

@patch(
    "app.agents.requirement_analysis_agent._provider.generate_json",
    side_effect=Exception("API timeout"),
)
def test_provider_error_does_not_crash(mock_generate):
    original_prefs = {"bedrooms": 3}
    state = _make_state(
        preferences=original_prefs,
        natural_language_prompt="I want a luxury villa with a rooftop pool.",
    )

    result = requirement_analysis_node(state)

    # State must not change on failure
    assert result.input_data.preferences == original_prefs, (
        "Preferences must be unchanged when provider errors"
    )

    # Failure must be logged
    ra_logs = [e for e in result.execution_log if e.agent_name == "RequirementAnalysisAgent"]
    assert len(ra_logs) == 1
    assert "failed" in ra_logs[0].action.lower()
