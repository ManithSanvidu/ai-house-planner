import pytest
from unittest.mock import patch, MagicMock

from app.design.visualization.layout_renderer import render_blueprint
from app.design.visualization.openai_visualization_service import OpenAIVisualizationService

def test_renderer_and_visualization():
    layout_json = {
        "rooms": [
            {"room_id": "r1", "room_type": "living_room", "name": "Living Room", "x": 0, "y": 0, "width": 10, "length": 10, "floor": 1},
            {"room_id": "r2", "room_type": "bedroom", "name": "Bedroom 1", "x": 10, "y": 0, "width": 10, "length": 10, "floor": 1},
            {"room_id": "r3", "room_type": "bedroom", "name": "Bedroom 2", "x": 0, "y": 10, "width": 10, "length": 10, "floor": 1},
            {"room_id": "r4", "room_type": "bedroom", "name": "Bedroom 3", "x": 10, "y": 10, "width": 10, "length": 10, "floor": 1},
            {"room_id": "r5", "room_type": "bath", "name": "Bathroom", "x": 20, "y": 0, "width": 5, "length": 5, "floor": 1},
        ]
    }
    
    # 1. Test renderer
    img_bytes = render_blueprint(layout_json)
    assert img_bytes is not None
    assert len(img_bytes) > 0
    # Can't easily OCR in test, but we can verify it doesn't crash
    
    # 2. Test OpenAIVisualizationService
    service = OpenAIVisualizationService(api_key="mock_key", workflow_id="test")
    
    with patch("app.design.visualization.openai_visualization_service.openai") as mock_openai:
        mock_response = MagicMock()
        mock_response.data = [MagicMock(url="http://example.com/img.png", b64_json=None)]
        mock_openai.images.edit.return_value = mock_response
        
        # Test valid room count
        result = service.generate_visualization(layout_json, expected_bedrooms=3, expected_bathrooms=1)
        
        assert result["status"] == "validated"
        
        # Verify images.edit was called with image reference
        mock_openai.images.edit.assert_called_once()
        call_kwargs = mock_openai.images.edit.call_args.kwargs
        assert "image" in call_kwargs
        assert call_kwargs["image"] == img_bytes
        assert "blueprint" in call_kwargs["prompt"].lower()

def test_visualization_validation_failure():
    layout_json = {
        "rooms": [
            {"room_id": "r1", "room_type": "bedroom", "name": "Bedroom 1", "x": 10, "y": 0, "width": 10, "length": 10, "floor": 1},
            {"room_id": "r2", "room_type": "bath", "name": "Bathroom", "x": 20, "y": 0, "width": 5, "length": 5, "floor": 1},
        ]
    }
    
    service = OpenAIVisualizationService(api_key="mock_key", workflow_id="test")
    
    # Expected 3 beds, input has 1 bed
    result = service.generate_visualization(layout_json, expected_bedrooms=3, expected_bathrooms=1)
    
    assert result["status"] == "failed"
    assert "validation" in result["error"].lower()
