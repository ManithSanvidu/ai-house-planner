from unittest.mock import MagicMock, patch

from app.design.generation.spatial_planner import _build_prompt, plan_spatial_program
from app.design.geometry.plot_constraints import PlotConstraints
from app.design.program.models import Requirements
from app.schemas.workflow_state import WorkflowState
from app.agents.design_agent import design_node
from app.services.construction_planning_service import construction_planning_node
from app.services.cost_estimation_service import cost_estimation_node
from app.services.ai_guard import execute_once, reset_ai_guard


def _plot():
    return PlotConstraints.model_validate({
        "land_size_perches": 20,
        "plot_width_ft": 60,
        "plot_length_ft": 80,
        "road_side": "south",
        "terrain_type": "flat",
        "setbacks": {"front": 10, "rear": 10, "left": 5, "right": 5},
    })


def _response():
    return {
        "concept": "Compact home",
        "floor_count": 1,
        "rooms": [
            {"id": "bed", "type": "bedroom", "floor": 1, "zone": "PRIVATE",
             "target_area_sqft": 120, "min_area_sqft": 100,
             "preferred_position": "REAR", "exterior_wall_required": True,
             "privacy_level": "HIGH"},
            {"id": "bath", "type": "bathroom", "floor": 1, "zone": "SERVICE",
             "target_area_sqft": 45, "min_area_sqft": 40,
             "preferred_position": "CENTER", "exterior_wall_required": False,
             "privacy_level": "HIGH"},
            {"id": "living", "type": "living", "floor": 1, "zone": "PUBLIC",
             "target_area_sqft": 180, "min_area_sqft": 140,
             "preferred_position": "FRONT", "exterior_wall_required": True,
             "privacy_level": "LOW"},
            {"id": "kitchen", "type": "kitchen", "floor": 1, "zone": "SERVICE",
             "target_area_sqft": 100, "min_area_sqft": 80,
             "preferred_position": "CENTER", "exterior_wall_required": True,
             "privacy_level": "LOW"},
        ],
        "adjacencies": [],
        "entrance": {"preferred_side": "SOUTH", "connect_to": "living"},
        "vertical_core": None,
        "reason_codes": [],
    }


def test_openai_spatial_program_response_is_cached(tmp_path, monkeypatch, capsys):
    monkeypatch.setenv("AI_RESPONSE_CACHE_DIR", str(tmp_path))
    provider = MagicMock(provider_name="openai", model_name="test-model")
    provider.provider_name = "openai"
    provider.model_name = "test-model"
    provider.generate_json.return_value = _response()
    req = Requirements(bedrooms=1, bathrooms=1, floors=1)

    with patch("app.design.generation.spatial_planner.get_available_design_provider", return_value=provider):
        first, first_meta = plan_spatial_program(req, _plot())
        second, second_meta = plan_spatial_program(req, _plot())

    assert first == second
    assert first_meta["cache_hit"] is False
    assert second_meta["cache_hit"] is True
    provider.generate_json.assert_called_once()
    output = capsys.readouterr().out
    assert "[AI CACHE] MISS" in output
    assert "[AI CACHE] HIT" in output


def test_design_strategy_prompt_is_compact_and_contains_no_geometry():
    prompt = _build_prompt(Requirements(bedrooms=3, bathrooms=2, floors=1), _plot(), 1500)
    assert len(prompt) < 1000
    assert '"bedrooms":3' in prompt
    assert '"land_size":20.0' in prompt
    assert "buildable_width" not in prompt
    assert "geometry" not in prompt.lower()
    assert "layout" not in prompt.lower()


def test_design_node_guard_skips_duplicate_ai_generation():
    state = WorkflowState(
        workflow_id="11111111-1111-1111-1111-111111111111",
        ai_design_generated=True,
        design_version=1,
        design_result={"rooms": []},
        current_agent="design",
    )
    with patch("app.design.generation.spatial_planner.plan_spatial_program") as planner:
        result = design_node(state)

    planner.assert_not_called()
    assert result.current_agent == "cost_estimation"


def test_construction_guard_reuses_existing_plan_without_persisting():
    state = WorkflowState(
        workflow_id="11111111-1111-1111-1111-111111111111",
        construction_plan_result={"phases": [{"name": "Foundation"}]},
        current_agent="construction_planning",
    )
    with patch("app.services.construction_planning_service.requests.patch") as persist:
        result = construction_planning_node(state)

    persist.assert_not_called()
    assert result.current_agent == "cost_estimation"


def test_cost_guard_reuses_existing_estimate_without_ai_or_persistence():
    state = WorkflowState(
        workflow_id="11111111-1111-1111-1111-111111111111",
        cost_result={"total_cost_lkr": 100_000},
        current_agent="cost_estimation",
    )
    with patch("app.services.cost_estimation_service._run_estimation") as estimate, \
         patch("app.services.cost_estimation_service._persist_cost_estimate") as persist:
        result = cost_estimation_node(state)

    estimate.assert_not_called()
    persist.assert_not_called()
    assert result.current_agent == "validation"


def test_ai_guard_reuses_one_successful_call_per_workflow_and_purpose(capsys):
    reset_ai_guard()
    operation = MagicMock(return_value={"result": "cached"})

    first, first_blocked = execute_once("workflow-guard", "design_strategy", operation)
    second, second_blocked = execute_once("workflow-guard", "design_strategy", operation)

    assert first == second == {"result": "cached"}
    assert first_blocked is False
    assert second_blocked is True
    operation.assert_called_once()
    assert "[AI GUARD] BLOCKED duplicate AI request" in capsys.readouterr().out
