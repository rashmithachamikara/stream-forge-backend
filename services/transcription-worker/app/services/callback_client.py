from __future__ import annotations

import json
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from app.config import WorkerSettings
from app.models.jobs import WorkerJob


class CallbackDeliveryError(RuntimeError):
    pass


class CallbackClient:
    def __init__(self, settings: WorkerSettings) -> None:
        self._settings = settings

    def send_completion(self, job: WorkerJob) -> None:
        payload = {
            "correlationId": job.correlation_id,
            "videoId": job.video_id,
            "workerJobId": job.id,
            "status": job.status,
            "language": job.detected_language or job.language,
            "artifacts": [
                {"kind": artifact.kind, "path": artifact.path}
                for artifact in job.artifact_records
            ],
            "failureReason": job.failure_reason,
            "provider": "local-faster-whisper",
            "model": job.options.get("model"),
        }
        self._post_json(job.callback_url, payload, job.callback_token)

    def _post_json(
        self, url: str, payload: dict[str, Any], callback_token: str | None
    ) -> None:
        body = json.dumps(payload).encode("utf-8")
        headers = {"Content-Type": "application/json"}

        if callback_token:
            headers["Authorization"] = f"Bearer {callback_token}"
        elif self._settings.callback_secret:
            headers[self._settings.callback_auth_header] = self._settings.callback_secret

        request = Request(url, data=body, headers=headers, method="POST")

        try:
            with urlopen(request, timeout=self._settings.callback_timeout_seconds) as response:
                if response.status < 200 or response.status >= 300:
                    raise CallbackDeliveryError(
                        f"Callback returned HTTP {response.status}."
                    )
        except HTTPError as exc:
            raise CallbackDeliveryError(
                f"Callback returned HTTP {exc.code}."
            ) from exc
        except URLError as exc:
            raise CallbackDeliveryError("Callback request failed.") from exc
