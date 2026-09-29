import pytest
from unittest.mock import patch, MagicMock

from app.schemas.workflow_state import WorkflowState, CoordinatorInput, ExecutionLogEntry
from app.agents.design_agent import design_node
from app.agents.requirement_analysis_agent import requirement_analysis_node
from app.design.generation.diversity import geometry_fingerprint
from app.design.catalogue.base_plan_library import load_base_plan_catalog
from app.schemas.design_result import DesignResult

@patch("app.agents.design_agent._submit_design", return_value="success")
@patch("app.providers.openai_provider.OpenAIProvider.generate_json")
def test_design_workflow_makes_zero_llm_calls(mock_generate, mock_submit):
    # Setup state for design_node
    state = WorkflowState(
        workflow_id="11111111-1111-1111-1111-111111111111",
        status="running",
        current_agent="design",
        input_data=CoordinatorInput(
            submission_id="123e4567-e89b-12d3-a456-426614174000",
            land_size_category="medium",
            land_size_perches=15.0,
            bedrooms=3,
            bathrooms=2,
            house_type="conventional"
        ),
        terrain_result={"terrain_type": "flat"}
    )

    # Execute design node
    new_state = design_node(state)
    
    # Assert generate_json is NOT called during design phase
    mock_generate.assert_not_called()
    
    # Verify design was successful
    if new_state.status == "failed":
        print("Failure Reason:", new_state.validation_result)
    assert new_state.status != "failed"
    assert new_state.design_result is not None
    assert new_state.current_agent == "cost_estimation"
    
    # Assert geometry fingerprint equality
    final_design = DesignResult.model_validate(new_state.design_result)
    assert final_design.geometry_fingerprint is not None
    




