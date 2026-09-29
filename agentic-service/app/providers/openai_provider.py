import json
from typing import Any

import requests
from pydantic import BaseModel

from app.config import OPENAI_API_KEY, OPENAI_MODEL, ENABLE_OPENAI
from app.providers.base_provider import (
    ModelProvider,
    ProviderAuthenticationError,
    ProviderMalformedResponseError,
    ProviderQuotaError,
    ProviderRateLimitError,
    ProviderTimeoutError,
    ProviderUnavailableError,
    ProviderUnknownError,
)
from app.services.openai_audit_logger import log_ai_call

MAX_INPUT_TOKENS = 4000
MAX_OUTPUT_TOKENS = 1000


class OpenAIProvider(ModelProvider):
    @property
    def provider_name(self) -> str:
        return "openai"

    @property
    def model_name(self) -> str:
        return OPENAI_MODEL

    def health_check(self) -> bool:
        return bool(OPENAI_API_KEY and OPENAI_API_KEY.startswith("sk-"))

    def generate_json(
        self,
        system_prompt: str,
        user_prompt: str,
        schema: type[BaseModel],
        max_tokens: int | None = None,
        purpose: str = "design_strategy",
        workflow_id: str | None = None,
    ) -> dict[str, Any]:
        # --- Global kill switch ---
        if not ENABLE_OPENAI:
            raise ProviderUnavailableError(
                "OpenAI is globally disabled (ENABLE_OPENAI=false). "
                "No HTTP request was made."
            )

        # --- Daily spending limit ---
        try:
            from app.services.ai_guard_db import check_daily_limit, DailyLimitExceeded
            check_daily_limit()
        except DailyLimitExceeded as exc:
            raise ProviderQuotaError(str(exc)) from exc
        except Exception:
            pass  # Non-fatal if DB is unavailable

        if not self.health_check():
            raise ProviderUnavailableError("OpenAI API key is missing or invalid.")

        url = "https://api.openai.com/v1/chat/completions"
        headers = {
            "Authorization": f"Bearer {OPENAI_API_KEY}",
            "Content-Type": "application/json"
        }

        # Use Structured Outputs (json_schema) for absolute reliability
        schema_dict = schema.model_json_schema()
        schema_dict["additionalProperties"] = False

        # OpenAI requires all properties to be explicitly listed in 'required'
        if "properties" in schema_dict:
            schema_dict["required"] = list(schema_dict["properties"].keys())

        payload = {
            "model": self.model_name,
            "max_tokens": max_tokens or 800,
            "temperature": 0.2,
            "response_format": {
                "type": "json_schema",
                "json_schema": {
                    "name": schema.__name__,
                    "schema": schema_dict,
                    "strict": True
                }
            },
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ]
        }

        # Diagnostic logging before request
        serialized = json.dumps(payload)
        char_count = len(serialized)
        est_tokens = char_count // 4
        print(
            f"[AI Request] purpose={purpose} workflow={workflow_id or 'n/a'} "
            f"characters={char_count} estimated_tokens={est_tokens} "
            f"messages={len(payload['messages'])}"
        )

        if est_tokens > MAX_INPUT_TOKENS:
            raise ProviderQuotaError(f"Request exceeds MAX_INPUT_TOKENS ({est_tokens} > {MAX_INPUT_TOKENS})")

        try:
            response = requests.post(url, headers=headers, json=payload, timeout=60)
        except requests.exceptions.Timeout:
            raise ProviderTimeoutError("OpenAI request timed out.")
        except requests.exceptions.RequestException as e:
            raise ProviderUnavailableError(f"OpenAI network error: {e}")

        if response.status_code != 200:
            data = response.json() if response.text else {}
            err_type = data.get("error", {}).get("type", "")
            err_code = data.get("error", {}).get("code", "")

            if response.status_code == 401:
                raise ProviderAuthenticationError(f"OpenAI Auth Error: {data}")
            elif response.status_code == 429:
                if "insufficient_quota" in err_code or "billing" in err_type:
                    raise ProviderQuotaError(f"OpenAI Quota Error: {data}")
                raise ProviderRateLimitError(f"OpenAI Rate Limit: {data}")
            elif response.status_code >= 500:
                raise ProviderUnavailableError(f"OpenAI Server Error: {data}")
            else:
                raise ProviderUnknownError(f"OpenAI HTTP {response.status_code}: {data}")

        data = response.json()
        if "choices" not in data or not data["choices"]:
            raise ProviderMalformedResponseError("OpenAI response missing 'choices'.")

        usage = data.get("usage", {})
        input_tokens = usage.get("prompt_tokens", 0)
        output_tokens = usage.get("completion_tokens", 0)
        total_tokens = usage.get("total_tokens", "unknown")
        cached_input_tokens = usage.get("prompt_tokens_details", {}).get("cached_tokens", 0)

        print(
            f"[Model Usage] provider=openai purpose={purpose} workflow={workflow_id or 'n/a'} "
            f"input_tokens={input_tokens} cached_input_tokens={cached_input_tokens} "
            f"output_tokens={output_tokens} total_tokens={total_tokens}"
        )

        # Persist cost log
        try:
            from app.services.ai_guard_db import log_ai_cost
            log_ai_cost(workflow_id, purpose, self.model_name, input_tokens, output_tokens)
            log_ai_call(workflow_id, purpose, self.model_name, input_tokens, output_tokens)
        except Exception:
            pass  # Non-fatal

        content = data["choices"][0]["message"]["content"]
        try:
            parsed = json.loads(content)
            return parsed
        except json.JSONDecodeError:
            raise ProviderMalformedResponseError("OpenAI returned invalid JSON string.")
