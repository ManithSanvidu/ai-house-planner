"""
Integration tests: requirement_analysis_node in the workflow.

A. Prompt exists → requirement_analysis executes and populates preferences
B. No prompt → requirement_analysis self-skips, state flows unchanged
C. Extracted preferences reach design node (preferences dict is correctly
   in place when design_node would read it)
"""
import uuid
from unittest.mock import patch

from app.agents.requirement_analysis_agent import requirement_analysis_node
from app.orchestration.workflow_router import coordinator_node
from app.schemas.workflow_state import CoordinatorInput, WorkflowState


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_state(
    preferences: dict | None = None,
    natural_language_prompt: str | None = None,
    manual_terrain_type: str | None = "flat",
) -> WorkflowState:
    return WorkflowState(
        workflow_id=str(uuid.uuid4()),
        status="running",
        input_data=CoordinatorInput(
            submission_id=str(uuid.uuid4()),
            land_size_perches=10.0,
            preferences=preferences or {},
            manual_terrain_type=manual_terrain_type,
            natural_language_prompt=natural_language_prompt,
        ),
    )


_MOCK_AI_OUTPUT = {
    "bedrooms": 3,
    "bathrooms": 2,
    "floors": 1,
    "architecturalStyle": "modern minimalist",
    "homeOffice": True,
    "accessibility": None,
    "separateDining": None,
    "parkingRequired": None,
    "masterEnsuite": None,
}


# ---------------------------------------------------------------------------
# A. Prompt exists → requirement_analysis executes
# ---------------------------------------------------------------------------

@patch(
    "app.agents.requirement_analysis_agent._provider.generate_json",
    return_value=_MOCK_AI_OUTPUT,
)
def test_prompt_triggers_extraction(mock_generate):
    state = _make_state(
        preferences={},
        natural_language_prompt="I need a modern minimalist home with a study room.",
    )

    result = requirement_analysis_node(state)

    # Verify the provider was called exactly once
    mock_generate.assert_called_once()

    # Verify AI preferences were injected
    prefs = result.input_data.preferences
    assert prefs.get("bedrooms") == 3
    assert prefs.get("style") == "modern minimalist"
    assert prefs.get("home_office") is True

    # Verify execution log was written
    ra_logs = [e for e in result.execution_log if e.agent_name == "RequirementAnalysisAgent"]
    assert len(ra_logs) == 1


# ---------------------------------------------------------------------------
# B. No prompt → requirement_analysis self-skips
# ---------------------------------------------------------------------------

def test_no_prompt_skips_extraction():
    state = _make_state(
        preferences={"bedrooms": 4, "floors": 2},
        natural_language_prompt=None,
    )

    with patch(
        "app.agents.requirement_analysis_agent._provider.generate_json"
    ) as mock_generate:
        result = requirement_analysis_node(state)
        mock_generate.assert_not_called()

    # Preferences must be unchanged
    assert result.input_data.preferences == {"bedrooms": 4, "floors": 2}
    # No log entry added
    ra_logs = [e for e in result.execution_log if e.agent_name == "RequirementAnalysisAgent"]
    assert not ra_logs


# ---------------------------------------------------------------------------
# C. Extracted preferences are present for design node to read
# ---------------------------------------------------------------------------

@patch(
    "app.agents.requirement_analysis_agent._provider.generate_json",
    return_value=_MOCK_AI_OUTPUT,
)
def test_extracted_preferences_available_for_design(mock_generate):
    """
    Simulates the coordinator → requirement_analysis sequence.
    After requirement_analysis, state.input_data.preferences must contain
    the merged result so that design_node's prepare_inputs() will pick it up.
    """
    state = _make_state(
        preferences={},  # start empty — all comes from NL
        natural_language_prompt="Build me a single-storey home with a home office.",
        manual_terrain_type="flat",
    )

    # Step 1: coordinator sets current_agent
    state = coordinator_node(state)
    assert state.current_agent == "design"  # manual_terrain skips land_analysis

    # Step 2: requirement_analysis runs and populates preferences
    state = requirement_analysis_node(state)

    # At this point design_node will call prepare_inputs(preferences=state.input_data.preferences)
    prefs = state.input_data.preferences

    # Core design fields must be present and correctly keyed for prepare_inputs()
    assert "bedrooms" in prefs, "bedrooms must be available for design"
    assert "floors" in prefs, "floors must be available for design"
    assert "style" in prefs, "style must be available for design"
    assert "home_office" in prefs, "home_office must be available for design"

    # current_agent must still route to design (requirement_analysis must not change it)
    assert state.current_agent == "design"


# ---------------------------------------------------------------------------
# D. Full coordinator → requirement_analysis sequence preserves routing
# ---------------------------------------------------------------------------

@patch(
    "app.agents.requirement_analysis_agent._provider.generate_json",
    return_value=_MOCK_AI_OUTPUT,
)
def test_coordinator_to_requirement_analysis_routing(mock_generate):
    """
    After coordinator sets current_agent to 'design' (manual terrain),
    requirement_analysis must NOT change current_agent.
    """
    state = _make_state(
        preferences={"bedrooms": 4},
        natural_language_prompt="I want an open-plan kitchen.",
        manual_terrain_type="flat",
    )

    state = coordinator_node(state)
    assert state.current_agent == "design"

    state = requirement_analysis_node(state)

    # routing must be preserved
    assert state.current_agent == "design"
    # explicit bedrooms=4 must override AI bedrooms=3
    assert state.input_data.preferences["bedrooms"] == 4
