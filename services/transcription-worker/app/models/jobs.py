from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any
from uuid import uuid4


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


@dataclass
class SegmentRecord:
    start_seconds: float
    end_seconds: float
    text: str


@dataclass
class ArtifactRecord:
    kind: str
    path: str


@dataclass
class WorkerJob:
    correlation_id: str
    video_id: str
    source_reference_type: str
    source_reference_value: str
    callback_url: str
    callback_token: str | None
    requested_formats: list[str]
    language: str | None
    options: dict[str, Any]
    id: str = field(default_factory=lambda: str(uuid4()))
    status: str = "queued"
    progress_percent: int = 0
    stage: str = "queued"
    message: str | None = None
    failure_reason: str | None = None
    detected_language: str | None = None
    artifact_records: list[ArtifactRecord] = field(default_factory=list)
    segments_file_path: str | None = None
    started_at: datetime | None = None
    completed_at: datetime | None = None
    created_at: datetime = field(default_factory=utc_now)

    def mark_running(self, stage: str, progress: int, message: str | None = None) -> None:
        self.status = "running"
        self.stage = stage
        self.progress_percent = progress
        self.message = message
        if self.started_at is None:
            self.started_at = utc_now()

    def mark_completed(
        self,
        detected_language: str | None,
        artifacts: list[ArtifactRecord],
        segments_file_path: str | None,
    ) -> None:
        self.status = "completed"
        self.stage = "completed"
        self.progress_percent = 100
        self.message = "Transcription completed."
        self.detected_language = detected_language
        self.artifact_records = artifacts
        self.segments_file_path = segments_file_path
        self.completed_at = utc_now()

    def mark_failed(self, reason: str) -> None:
        self.status = "failed"
        self.stage = "failed"
        self.progress_percent = 100
        self.message = "Transcription failed."
        self.failure_reason = reason
        self.completed_at = utc_now()

    def to_status_dict(self) -> dict[str, Any]:
        return {
            "jobId": self.id,
            "correlationId": self.correlation_id,
            "videoId": self.video_id,
            "status": self.status,
            "progressPercent": self.progress_percent,
            "stage": self.stage,
            "message": self.message,
            "language": self.detected_language or self.language,
            "startedAt": self.started_at.isoformat() if self.started_at else None,
            "completedAt": self.completed_at.isoformat() if self.completed_at else None,
            "artifacts": [
                {"kind": artifact.kind, "path": artifact.path}
                for artifact in self.artifact_records
            ],
            "failureReason": self.failure_reason,
        }


@dataclass
class TranscriptionResult:
    detected_language: str | None
    segments: list[SegmentRecord]
    artifact_paths: dict[str, Path]
