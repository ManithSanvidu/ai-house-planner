import asyncio
from app.workflows.house_planning_graph import app_graph
from app.schemas.workflow_state import WorkflowState, CoordinatorInput

def test_graph():
    state = WorkflowState(
        workflow_id="12345678-1234-1234-1234-123456789012",
        status="running",
        current_agent="coordinator",
        approval_status="not_requested",
        terrain_result={"terrain_type": "flat", "notable_features": []},
        land_size=25.0,
        input_data=CoordinatorInput(
            submission_id="12345678-1234-1234-1234-123456789012",
            land_size_perches=25.0,
            preferences={"bedrooms": 3, "bathrooms": 2, "floors": 1, "style": "Modern Tropical"},
            plot_constraints={"plot_width_ft": 60, "plot_length_ft": 80}
        )
    )
    
    # Mock submit design
    import app.agents.design_agent as da
    da._submit_design = lambda s: "success"
    
    print("Executing app_graph...")
    try:
        final_state = app_graph.invoke(state)
        print(f"Final Status: {final_state.get('status', 'unknown')}")
        print(f"Rooms: {len(final_state.get('design_result', {}).get('rooms', [])) if final_state.get('design_result') else 0}")
        print(f"Visualization URL: {final_state.get('design_result', {}).get('ai_visualization', {}).get('image_url') if final_state.get('design_result') else 'None'}")
    except Exception as e:
        print(f"Error during graph execution: {e}")

if __name__ == "__main__":
    test_graph()
