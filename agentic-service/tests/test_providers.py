import os
from unittest.mock import patch, MagicMock
from app.providers.provider_factory import get_available_design_provider
from app.providers.openai_provider import OpenAIProvider

def test_provider_fallback():
    # Mock environment to have no API keys
    with patch("app.providers.openai_provider.OPENAI_API_KEY", ""), \
         patch("app.providers.ollama_provider.OLLAMA_BASE_URL", ""):
        provider = get_available_design_provider()
        assert provider is None, "Should return None (procedural fallback) if no keys are set"

    # Mock environment with OpenAI key
    with patch("app.providers.openai_provider.OPENAI_API_KEY", "sk-mock-key"), \
         patch("app.providers.ollama_provider.OLLAMA_BASE_URL", ""):
        provider = get_available_design_provider()
        assert isinstance(provider, OpenAIProvider), "Should return OpenAI if configured"

if __name__ == "__main__":
    test_provider_fallback()
    print("Provider factory tests passed!")
