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
    shared_root: Path
    output_root: Path
    shared_media_root: Path
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
        repo_root = _find_repo_root(Path(__file__).resolve())
        default_shared_root = repo_root / "data"

        shared_root = _resolve_path_override(
            os.getenv("STREAMFORGE_LOCAL_STORAGE_ROOT")
            or os.getenv("TRANSCRIPTION_WORKER_LOCAL_STORAGE_ROOT")
            or os.getenv("STREAMFORGE_SHARED_ROOT")
            or os.getenv("TRANSCRIPTION_WORKER_SHARED_ROOT"),
            default_shared_root,
            repo_root,
        )
        shared_media_root = _resolve_path_override(
            os.getenv("TRANSCRIPTION_WORKER_SHARED_MEDIA_ROOT"),
            shared_root / "uploads",
            repo_root,
        )
        output_root = _resolve_path_override(
            os.getenv("TRANSCRIPTION_WORKER_OUTPUT_ROOT"),
            shared_root / "transcription-output",
            repo_root,
        )

        return WorkerSettings(
            shared_root=shared_root,
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


def _find_repo_root(start_path: Path) -> Path:
    current = start_path
    for candidate in (current, *current.parents):
        if (candidate / "StreamForge.sln").exists():
            return candidate

    return start_path.parent


def _resolve_path_override(value: str | None, default_path: Path, repo_root: Path) -> Path:
    if value is None or not value.strip():
        return default_path.resolve()

    configured = Path(value)
    if configured.is_absolute():
        return configured.resolve()

    return (repo_root / configured).resolve()
