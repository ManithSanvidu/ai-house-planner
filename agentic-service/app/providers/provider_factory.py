
from app.config import DESIGN_PROVIDER_ORDER
from app.providers.base_provider import ModelProvider
from app.providers.ollama_provider import OllamaProvider
from app.providers.openai_provider import OpenAIProvider

_PROVIDERS: dict[str, ModelProvider] = {
    "openai": OpenAIProvider(),
    "ollama": OllamaProvider(),
}

def get_provider(name: str) -> ModelProvider | None:
    """Retrieve a specific provider by name."""
    return _PROVIDERS.get(name.lower())

def get_available_design_provider() -> ModelProvider | None:
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


def get_next_design_provider(after_provider: str) -> ModelProvider | None:
    """Return the next healthy configured provider after a failed runtime call."""
    ordered = list(dict.fromkeys([*DESIGN_PROVIDER_ORDER, *_PROVIDERS.keys()]))
    try:
        start = ordered.index(after_provider.lower()) + 1
    except ValueError:
        return None
    for provider_name in ordered[start:]:
        provider = get_provider(provider_name)
        if provider and provider.health_check():
            return provider
    return None
