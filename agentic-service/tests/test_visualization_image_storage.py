from unittest.mock import MagicMock, patch

import pytest
import requests

from app.services.visualization_image_storage import (
    VisualizationImageStorage,
    VisualizationStorageError,
)


def _storage():
    return VisualizationImageStorage(
        supabase_url="https://project.supabase.co",
        service_role_key="server-secret",
        bucket="ai-visualizations",
    )


def test_upload_returns_workflow_scoped_object_key_without_exposing_credentials():
    response = MagicMock()
    response.raise_for_status.return_value = None
    with patch("app.services.visualization_image_storage.requests.post", return_value=response) as upload:
        object_key = _storage().upload_image("workflow-123", b"png-bytes")

    assert object_key.startswith("visualizations/workflow-123/")
    assert object_key.endswith(".png")
    assert "localhost" not in object_key
    assert "server-secret" not in object_key
    assert upload.call_args.kwargs["data"] == b"png-bytes"
    assert upload.call_args.kwargs["headers"]["Content-Type"] == "image/png"


def test_upload_network_failure_is_sanitized():
    with patch(
        "app.services.visualization_image_storage.requests.post",
        side_effect=requests.ConnectionError("private provider detail"),
    ):
        with pytest.raises(VisualizationStorageError) as error:
            _storage().upload_image("workflow-123", b"png-bytes")

    assert str(error.value) == "Generated visualization could not be stored."
    assert "private provider detail" not in str(error.value)


@pytest.mark.parametrize("workflow_id", ["", "bad/workflow"])
def test_upload_rejects_invalid_workflow_identifier(workflow_id):
    with pytest.raises(VisualizationStorageError):
        _storage().upload_image(workflow_id, b"png-bytes")
