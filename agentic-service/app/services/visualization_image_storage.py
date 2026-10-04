"""Private Supabase Storage persistence for generated visualization images."""
from __future__ import annotations

import uuid

import requests

from app.config import (
    SUPABASE_AI_VISUALIZATION_BUCKET,
    SUPABASE_SERVICE_ROLE_KEY,
    SUPABASE_URL,
)


class VisualizationStorageError(RuntimeError):
    """A sanitized failure raised when durable visualization storage fails."""


class VisualizationImageStorage:
    def __init__(
        self,
        supabase_url: str = SUPABASE_URL,
        service_role_key: str = SUPABASE_SERVICE_ROLE_KEY,
        bucket: str = SUPABASE_AI_VISUALIZATION_BUCKET,
    ):
        self.supabase_url = str(supabase_url or "").strip().rstrip("/")
        self.service_role_key = str(service_role_key or "").strip()
        self.bucket = str(bucket or "").strip()
        if not self.supabase_url:
            raise VisualizationStorageError("Visualization storage is not configured.")
        if not self.service_role_key:
            raise VisualizationStorageError("Visualization storage credentials are not configured.")
        if not self.bucket or "/" in self.bucket:
            raise VisualizationStorageError("Visualization storage bucket is invalid.")

    def upload_image(self, workflow_id: str, image_bytes: bytes, extension: str = "png") -> str:
        if not image_bytes:
            raise VisualizationStorageError("Generated visualization image was empty.")
        safe_workflow_id = str(workflow_id).strip()
        if not safe_workflow_id or "/" in safe_workflow_id:
            raise VisualizationStorageError("Visualization workflow identifier is invalid.")
        safe_extension = str(extension).strip().lower().lstrip(".")
        if safe_extension != "png":
            raise VisualizationStorageError("Visualization image format is unsupported.")

        object_key = f"visualizations/{safe_workflow_id}/{uuid.uuid4()}.{safe_extension}"
        encoded_key = "/".join(requests.utils.quote(segment, safe="") for segment in object_key.split("/"))
        encoded_bucket = requests.utils.quote(self.bucket, safe="")
        try:
            response = requests.post(
                f"{self.supabase_url}/storage/v1/object/{encoded_bucket}/{encoded_key}",
                data=image_bytes,
                headers={
                    "Authorization": f"Bearer {self.service_role_key}",
                    "apikey": self.service_role_key,
                    "Content-Type": "image/png",
                    "x-upsert": "false",
                },
                timeout=30,
            )
            response.raise_for_status()
        except requests.RequestException as exc:
            raise VisualizationStorageError("Generated visualization could not be stored.") from exc
        return object_key

    def delete_image(self, object_key: str) -> None:
        """Best-effort API for future replacement cleanup; not used by the current cache flow."""
        if not object_key or object_key.startswith(("http://", "https://", "/")):
            return
        try:
            response = requests.delete(
                f"{self.supabase_url}/storage/v1/object/{requests.utils.quote(self.bucket, safe='')}",
                json={"prefixes": [object_key]},
                headers={
                    "Authorization": f"Bearer {self.service_role_key}",
                    "apikey": self.service_role_key,
                },
                timeout=30,
            )
            response.raise_for_status()
        except requests.RequestException as exc:
            raise VisualizationStorageError("Stored visualization could not be deleted.") from exc
