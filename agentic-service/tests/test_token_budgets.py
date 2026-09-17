import pytest
import json
from unittest.mock import patch, MagicMock

from app.tools.layout_generation_tool import generate_layout
from app.schemas.design_strategy import DesignStrategy

def test_generate_another_uses_cache():
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
        # Provide a mock valid return for the strategy
        mock_gen.return_value = {
            "topology_family": "COMPACT_RECTANGLE",
            "public_zone": "open",
            "private_zone": "clustered",
            "service_zone": "grouped",
            "circulation_strategy": "direct",
            "entrance_side": "south",
            "bedroom_strategy": "standard",
            "kitchen_relationship": "open",
            "wet_zone_strategy": "grouped",
            "style_intent": "modern"
        }
        
        from app.tools.layout_generation_tool import _strategy_cache
        _strategy_cache.clear()
        
        # Call 1: Should hit the provider
        res1 = generate_layout(10.0, 'flat', preferences, design_seed=42)
        assert mock_gen.call_count == 1
        
        # Verify prompt does not contain geometry fields
        args, kwargs = mock_gen.call_args
        user_prompt1 = args[1]
        assert "x" not in user_prompt1 or '"x"' not in user_prompt1, "Payload must not contain 'x' geometry field"
        assert "width_array" not in user_prompt1, "Payload must not contain arrays of geometry"
        
        # Call 2: Change ONLY design seed, simulating "Generate Another"
        preferences['design_seed'] = 43
        res2 = generate_layout(10.0, 'flat', preferences, design_seed=43)
        
        args, kwargs = mock_gen.call_args
        user_prompt2 = args[1]
        
        if user_prompt1 != user_prompt2:
            print("Prompt 1:", user_prompt1)
            print("Prompt 2:", user_prompt2)
        
        # Call count should STILL be 1 because of the cache!
        assert mock_gen.call_count == 1, "Generate Another should use 0 LLM calls by hitting cache"

def test_simple_revision_uses_cache():
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
            "topology_family": "COMPACT_RECTANGLE",
            "public_zone": "open",
            "private_zone": "clustered",
            "service_zone": "grouped",
            "circulation_strategy": "direct",
            "entrance_side": "south",
            "bedroom_strategy": "standard",
            "kitchen_relationship": "open",
            "wet_zone_strategy": "grouped",
            "style_intent": "modern"
        }
        
        from app.tools.layout_generation_tool import _strategy_cache
        _strategy_cache.clear()
        
        # Initial generation
        generate_layout(10.0, 'flat', preferences, design_seed=42)
        assert mock_gen.call_count == 1
        
        # Simple revision: "make living room bigger"
        generate_layout(10.0, 'flat', preferences, revision_reason="make living room bigger", design_seed=42)
        
        # Call count should STILL be 1 because living_area_scale is excluded from strategy hash
        assert mock_gen.call_count == 1, "Simple revision should use 0 LLM calls by hitting cache"
