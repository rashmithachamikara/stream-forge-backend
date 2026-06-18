from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


def _get_bool(name: str, default: bool) -> bool:
    value = os.getenv(name)
    if value is None:
        return default
    return value.strip().lower() in {"1", "true", "yes", "on"}


@dataclass(frozen=True)
class WorkerSettings:
    output_root: Path
    shared_media_root: Path | None
    callback_timeout_seconds: int
    callback_auth_header: str
    callback_secret: str | None
    default_model: str
    default_device: str
    default_compute_type: str
    default_beam_size: int
    enable_vad: bool
    enable_word_timestamps: bool

    @staticmethod
    def load() -> "WorkerSettings":
        output_root = Path(
            os.getenv("TRANSCRIPTION_WORKER_OUTPUT_ROOT", "./data/transcription-output")
        ).resolve()

        shared_media_root_value = os.getenv("TRANSCRIPTION_WORKER_SHARED_MEDIA_ROOT")
        shared_media_root = (
            Path(shared_media_root_value).resolve()
            if shared_media_root_value and shared_media_root_value.strip()
            else None
        )

        return WorkerSettings(
            output_root=output_root,
            shared_media_root=shared_media_root,
            callback_timeout_seconds=int(
                os.getenv("TRANSCRIPTION_WORKER_CALLBACK_TIMEOUT_SECONDS", "30")
            ),
            callback_auth_header=os.getenv(
                "TRANSCRIPTION_WORKER_CALLBACK_AUTH_HEADER",
                "X-StreamForge-Worker-Secret",
            ),
            callback_secret=os.getenv("TRANSCRIPTION_WORKER_CALLBACK_SECRET"),
            default_model=os.getenv("TRANSCRIPTION_WORKER_DEFAULT_MODEL", "small"),
            default_device=os.getenv("TRANSCRIPTION_WORKER_DEFAULT_DEVICE", "cpu"),
            default_compute_type=os.getenv(
                "TRANSCRIPTION_WORKER_DEFAULT_COMPUTE_TYPE", "int8"
            ),
            default_beam_size=int(
                os.getenv("TRANSCRIPTION_WORKER_DEFAULT_BEAM_SIZE", "5")
            ),
            enable_vad=_get_bool("TRANSCRIPTION_WORKER_ENABLE_VAD", True),
            enable_word_timestamps=_get_bool(
                "TRANSCRIPTION_WORKER_WORD_TIMESTAMPS", False
            ),
        )
