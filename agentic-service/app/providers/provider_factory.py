from typing import Optional, Dict
from app.config import DESIGN_PROVIDER_ORDER
from app.providers.base_provider import ModelProvider
from app.providers.openai_provider import OpenAIProvider
from app.providers.ollama_provider import OllamaProvider

_PROVIDERS: Dict[str, ModelProvider] = {
    "openai": OpenAIProvider(),
    "ollama": OllamaProvider(),
}

def get_provider(name: str) -> Optional[ModelProvider]:
    """Retrieve a specific provider by name."""
    return _PROVIDERS.get(name.lower())

def get_available_design_provider() -> Optional[ModelProvider]:
    """
    Iterate through DESIGN_PROVIDER_ORDER.
    Return the first provider that passes its health check.
    If none are available, return None (triggering procedural fallback).
    """
    for provider_name in DESIGN_PROVIDER_ORDER:
        provider = get_provider(provider_name)
        if provider and provider.health_check():
            return provider
            
    # As a last resort, check if ANY configured provider is healthy
    for name, provider in _PROVIDERS.items():
        if provider.health_check():
            print(f"[Provider Factory] Primary providers unavailable. Falling back to {name}.")
            return provider

    print("[Provider Factory] No LLM providers available. Falling back to procedural strategy.")
    return None
