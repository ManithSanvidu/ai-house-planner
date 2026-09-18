import pytest
import json
from unittest.mock import patch, MagicMock

from app.design.candidate_generator import GenerationFailure
from app.tools.layout_generation_tool import generate_layout

def test_generate_another_calls_ai_once_per_request():
    preferences = {
        'bedrooms': 3,
        'style': 'modern',
        'design_seed': 42,
        'plot_constraints': {
            'plot_width_ft': 50.0,
            'plot_length_ft': 50.0
        }
    }
    
    with patch('app.providers.openai_provider.OpenAIProvider.generate_json') as mock_gen:
        mock_gen.return_value = {
            "selected_plan_code": "INVALID",
            "alternative_plan_codes": [],
            "design_intent": {
                "public_zone_orientation": "south",
                "private_zone_orientation": "north",
                "service_zone_orientation": "west",
                "privacy_priority": "balanced",
                "circulation_preference": "short_central_hall"
            },
            "adaptations": {
                "mirror_horizontal": False,
                "mirror_vertical": False,
                "rotation_degrees": 0,
                "living_scale": 1.0,
                "bedroom_scale": 1.0,
                "entrance_side": "south",
                "preserve_stair_core": True,
                "preserve_wet_core": True
            },
            "reason_codes": ["plot_fit"]
        }

        result = generate_layout(10.0, 'flat', preferences, design_seed=42)
        assert result.candidate_summary['generation_mode'] == 'deterministic_template_selection'
        assert mock_gen.call_count == 1
        
        # Verify prompt does not contain geometry fields
        args, kwargs = mock_gen.call_args
        user_prompt1 = args[1]
        assert "x" not in user_prompt1 or '"x"' not in user_prompt1, "Payload must not contain 'x' geometry field"
        assert "width_array" not in user_prompt1, "Payload must not contain arrays of geometry"
        
        # Call 2: Change ONLY design seed, simulating "Generate Another"
        preferences['design_seed'] = 43
        result2 = generate_layout(10.0, 'flat', preferences, design_seed=43)
        assert result2.candidate_summary['generation_mode'] == 'deterministic_template_selection'
        assert mock_gen.call_count == 2
        
        args, kwargs = mock_gen.call_args
        user_prompt2 = args[1]
        
        if user_prompt1 != user_prompt2:
            print("Prompt 1:", user_prompt1)
            print("Prompt 2:", user_prompt2)
        
        # Each request still makes one AI decision call.
        assert mock_gen.call_count == 2

    def test_simple_revision_calls_ai_again():
        preferences = {
            'bedrooms': 3,
            'style': 'modern',
            'design_seed': 42,
            'plot_constraints': {
                'plot_width_ft': 50.0,
                'plot_length_ft': 50.0
            }
        }
    
        with patch('app.providers.openai_provider.OpenAIProvider.generate_json') as mock_gen:
            mock_gen.return_value = {
                "selected_plan_code": "INVALID",
                "alternative_plan_codes": [],
                "design_intent": {
                    "public_zone_orientation": "south",
                    "private_zone_orientation": "north",
                    "service_zone_orientation": "west",
                    "privacy_priority": "balanced",
                    "circulation_preference": "short_central_hall"
                },
                "adaptations": {
                    "mirror_horizontal": False,
                    "mirror_vertical": False,
                    "rotation_degrees": 0,
                    "living_scale": 1.0,
                    "bedroom_scale": 1.0,
                    "entrance_side": "south",
                    "preserve_stair_core": True,
                    "preserve_wet_core": True
                },
                "reason_codes": ["plot_fit"]
            }

            with pytest.raises(GenerationFailure):
                generate_layout(10.0, 'flat', preferences, design_seed=42)
            assert mock_gen.call_count == 1
            
            # Simple revision: "make living room bigger"
            with pytest.raises(GenerationFailure):
                generate_layout(10.0, 'flat', preferences, revision_reason="make living room bigger", design_seed=42)
            
            # The revision still makes exactly one additional AI call.
            assert mock_gen.call_count == 2
