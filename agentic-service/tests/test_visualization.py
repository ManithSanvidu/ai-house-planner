import pytest
import base64
from app.design.visualization.visualization_agent import VisualizationAgent
from app.design.visualization.openai_visualization_service import OpenAIVisualizationService
import json
from types import SimpleNamespace
from unittest.mock import MagicMock, patch

from app.design.visualization.visualization_agent import visualization_node
from app.design.visualization.visualization_agent import _save_generated_image
from app.design.visualization.visualization_agent import _persist_visualization
from app.services.visualization_image_storage import VisualizationStorageError

def test_visualization_uses_generated_layout(monkeypatch):
    # Test that the visualization service receives the layout and produces a valid mock response
    layout_json = {
        "floor_count": 1,
        "rooms": [{"name": "Living Room", "width": 10, "length": 10}]
    }
    
    monkeypatch.delenv("OPENAI_API_KEY", raising=False)
    # An absent API key must not claim that an AI image was generated.
    agent = VisualizationAgent(api_key=None)
    result = agent.process(layout_json)
    
    assert result["status"] == "failed"
    assert result["image_url"] is None
    assert "OpenAI API key or library missing" in result["error"]

def test_ai_failure_does_not_break_workflow():
    from unittest.mock import patch
    agent = VisualizationAgent(api_key="mock_key")
    with patch("app.design.visualization.openai_visualization_service.openai") as mock_openai:
        mock_openai.images.generate.side_effect = Exception("OpenAI outage")
        mock_openai.images.edit.side_effect = Exception("OpenAI outage")
        
        # It should not raise an exception, but return a failed status
        result = agent.process({"rooms": []})
        assert result["status"] == "failed"
        assert result["image_url"] is None
        assert "OpenAI outage" in result["error"]


from app.schemas.workflow_state import CoordinatorInput

def _make_state(workflow_id="workflow-1"):
    return SimpleNamespace(
        workflow_id=workflow_id,
        execution_log=[],
        input_data=CoordinatorInput(
            submission_id="00000000-0000-0000-0000-000000000001",
            land_size_category="medium",
            land_size_perches=10.0,
            bedrooms=1,
            bathrooms=1,
            house_type="conventional"
        ),
        design_result={"rooms": [{"room_type": "bedroom"}, {"room_type": "bathroom"}, {"room_type": "living"}, {"room_type": "kitchen"}, {"room_type": "dining"}]}
    )

def test_visualization_node_reuses_persisted_image():
    state = _make_state()
    with patch("app.design.visualization.visualization_agent._existing_visualization",
               return_value="https://example.com/existing.png"), \
         patch("app.design.visualization.visualization_agent.VisualizationAgent") as agent, \
         patch("app.design.visualization.visualization_agent._save_generated_image") as upload:
        visualization_node(state)

    agent.assert_not_called()
    upload.assert_not_called()
    assert state.design_result["ai_visualization"]["image_url"].endswith("existing.png")


def test_visualization_node_persists_new_image():
    state = _make_state()
    agent = MagicMock()
    agent.process.return_value = {"status": "validated", "image_url": "https://example.com/new.png"}
    with patch("app.design.visualization.visualization_agent._existing_visualization", return_value=None), \
         patch("app.design.visualization.visualization_agent.ENABLE_AI_VISUALIZATION", True), \
         patch("app.design.visualization.visualization_agent.VisualizationAgent", return_value=agent), \
         patch("app.design.visualization.visualization_agent.execute_once", lambda wf, purp, fn: (fn(), False)), \
         patch("app.design.visualization.visualization_agent._save_generated_image", return_value="visualizations/workflow-1/stable.png"), \
         patch("app.design.visualization.visualization_agent._persist_visualization") as persist:
        visualization_node(state)

    persist.assert_called_once_with("workflow-1", "visualizations/workflow-1/stable.png")
    agent.process.assert_called_once_with(
        state.design_result,
        1,
        1,
        "medium",
        "conventional",
    )
    assert state.design_result["ai_visualization"]["image_url"].endswith("stable.png")


def test_visualization_disabled_never_calls_openai():
    state = _make_state("workflow-disabled")
    with patch("app.design.visualization.visualization_agent._existing_visualization", return_value=None), \
         patch("app.design.visualization.visualization_agent.ENABLE_AI_VISUALIZATION", False), \
         patch("app.design.visualization.visualization_agent.VisualizationAgent") as agent:
        visualization_node(state)

    agent.assert_not_called()
    assert state.design_result["ai_visualization"]["status"] == "disabled"


def test_temporary_provider_url_is_downloaded_and_uploaded_to_storage():
    response = MagicMock(content=b"png-bytes")
    response.raise_for_status.return_value = None
    storage = MagicMock()
    storage.upload_image.return_value = "visualizations/workflow-1/generated.png"
    with patch("app.design.visualization.visualization_agent.requests.get", return_value=response):
        object_key = _save_generated_image(
            {"image_url": "https://temporary.example/image.png"}, "workflow-1", storage
        )

    assert object_key == "visualizations/workflow-1/generated.png"
    storage.upload_image.assert_called_once_with("workflow-1", b"png-bytes", extension="png")


def test_base64_provider_image_is_uploaded_without_local_or_network_download():
    storage = MagicMock()
    storage.upload_image.return_value = "visualizations/workflow-2/generated.png"
    encoded = base64.b64encode(b"base64-png-bytes").decode()

    with patch("app.design.visualization.visualization_agent.requests.get") as download:
        object_key = _save_generated_image({"image_b64": encoded}, "workflow-2", storage)

    assert object_key == "visualizations/workflow-2/generated.png"
    assert "localhost" not in object_key
    storage.upload_image.assert_called_once_with(
        "workflow-2", b"base64-png-bytes", extension="png"
    )
    download.assert_not_called()


def test_storage_upload_failure_marks_visualization_failed_without_persisting_key():
    state = _make_state("workflow-storage-failure")
    agent = MagicMock()
    agent.process.return_value = {"status": "validated", "image_b64": "cG5n"}
    with patch("app.design.visualization.visualization_agent._existing_visualization", return_value=None), \
         patch("app.design.visualization.visualization_agent.ENABLE_AI_VISUALIZATION", True), \
         patch("app.design.visualization.visualization_agent.VisualizationAgent", return_value=agent), \
         patch("app.design.visualization.visualization_agent.execute_once", lambda wf, purp, fn: (fn(), False)), \
         patch("app.design.visualization.visualization_agent._save_generated_image", side_effect=VisualizationStorageError("safe failure")), \
         patch("app.design.visualization.visualization_agent._persist_visualization") as persist, \
         patch("app.design.visualization.visualization_agent._persist_visualization_status") as persist_status:
        visualization_node(state)

    persist.assert_not_called()
    persist_status.assert_called_once_with("workflow-storage-failure", "failed")
    assert state.design_result["ai_visualization"]["status"] == "failed"
    assert state.design_result["ai_visualization"]["image_url"] is None


def test_fastapi_mounts_visualization_storage():
    from fastapi.testclient import TestClient
    from app.main import app
    from app.config import VISUALIZATIONS_DIR

    filename = "static-serving-test.png"
    path = VISUALIZATIONS_DIR / filename
    path.write_bytes(b"png-route-bytes")
    try:
        response = TestClient(app).get(f"/visualizations/{filename}")
    finally:
        path.unlink(missing_ok=True)

    assert response.status_code == 200
    assert response.content == b"png-route-bytes"


def test_stable_url_is_persisted_as_completed():
    response = MagicMock(ok=True)
    with patch("app.design.visualization.visualization_agent.requests.patch", return_value=response) as request:
        _persist_visualization("workflow-1", "visualizations/workflow-1/stable.png")

    assert request.call_args.kwargs["json"] == {
        "imageUrl": "visualizations/workflow-1/stable.png",
        "status": "completed",
    }
