import pytest
import json
from unittest.mock import patch, MagicMock

from app.services.ai_guard_db import create_ai_execution_key, execute_once_db
from app.providers.openai_provider import OpenAIProvider, MAX_INPUT_TOKENS
from app.providers.base_provider import ProviderQuotaError
from pydantic import BaseModel

class DummySchema(BaseModel):
    test: str

def test_cache_key_generation():
    payload1 = {"inventory": [{"name": "cement", "qty": 10}]}
    payload2 = {"inventory": [{"name": "cement", "qty": 10}]}
    payload3 = {"inventory": [{"name": "cement", "qty": 20}]}
    
    key1 = create_ai_execution_key("proj1", "plan", payload1)
    key2 = create_ai_execution_key("proj1", "plan", payload2)
    key3 = create_ai_execution_key("proj1", "plan", payload3)
    
    assert key1 == key2
    assert key1 != key3

@patch("app.services.ai_guard_db._execute_with_db")
def test_db_unavailable_blocks_openai(mock_execute):
    mock_execute.side_effect = Exception("DB Connection Failed")
    
    def operation():
        return {"called": True}
        
    result, cached = execute_once_db("proj1", "plan", operation)
    assert not cached
    assert result == {"status": "blocked", "reason": "AI protection unavailable"}

@patch("requests.post")
def test_token_limit_blocks_huge_prompts(mock_post):
    provider = OpenAIProvider()
    provider.health_check = MagicMock(return_value=True)
    
    # 5000 characters is ~1250 tokens, but we want > 4000 tokens
    # so we need > 16000 characters
    huge_prompt = "test " * 4000
    
    with pytest.raises(ProviderQuotaError) as exc_info:
        provider.generate_json("sys", huge_prompt, DummySchema)
        
    assert "exceeds MAX_INPUT_TOKENS" in str(exc_info.value)
