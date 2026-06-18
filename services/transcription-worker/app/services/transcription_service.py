from __future__ import annotations

import json
import os
import tempfile
from pathlib import Path
from urllib.parse import urlparse
from urllib.request import urlretrieve

from faster_whisper import WhisperModel

from app.config import WorkerSettings
from app.models.jobs import SegmentRecord, TranscriptionResult, WorkerJob


class UnsupportedSourceReferenceError(RuntimeError):
    pass


class TranscriptionService:
    def __init__(self, settings: WorkerSettings) -> None:
        self._settings = settings

    def transcribe(self, job: WorkerJob) -> TranscriptionResult:
        resolved_media_path, cleanup_path = self._resolve_source(job)
        try:
            output_dir = self._settings.output_root / job.id
            output_dir.mkdir(parents=True, exist_ok=True)

            model = WhisperModel(
                job.options["model"],
                device=job.options["device"],
                compute_type=job.options["compute_type"],
            )

            segments_iter, info = model.transcribe(
                str(resolved_media_path),
                language=job.language or None,
                beam_size=job.options["beam_size"],
                vad_filter=job.options["enable_vad"],
                word_timestamps=job.options["enable_word_timestamps"],
            )

            segments = [
                SegmentRecord(
                    start_seconds=float(segment.start),
                    end_seconds=float(segment.end),
                    text=segment.text.strip(),
                )
                for segment in segments_iter
            ]

            artifact_paths: dict[str, Path] = {}
            artifact_paths["segments.json"] = self._write_segments_json(output_dir, segments)

            if "vtt" in job.requested_formats:
                artifact_paths["captions.vtt"] = self._write_vtt(output_dir, segments)

            if "srt" in job.requested_formats:
                artifact_paths["captions.srt"] = self._write_srt(output_dir, segments)

            return TranscriptionResult(
                detected_language=getattr(info, "language", None),
                segments=segments,
                artifact_paths=artifact_paths,
            )
        finally:
            if cleanup_path and cleanup_path.exists():
                cleanup_path.unlink(missing_ok=True)

    def _resolve_source(self, job: WorkerJob) -> tuple[Path, Path | None]:
        source_type = job.source_reference_type
        source_value = job.source_reference_value

        if source_type == "local_path":
            path = Path(source_value)
            if not path.is_absolute():
                if self._settings.shared_media_root is None:
                    raise UnsupportedSourceReferenceError(
                        "Relative local paths require TRANSCRIPTION_WORKER_SHARED_MEDIA_ROOT."
                    )
                path = self._settings.shared_media_root / path

            if not path.exists():
                raise FileNotFoundError(f"Source media path does not exist: {path}")

            return path.resolve(), None

        if source_type in {"url", "presigned_url"}:
            parsed = urlparse(source_value)
            suffix = Path(parsed.path).suffix or ".media"
            handle, temp_name = tempfile.mkstemp(prefix="streamforge-media-", suffix=suffix)
            os.close(handle)
            Path(temp_name).unlink(missing_ok=True)
            urlretrieve(source_value, temp_name)
            return Path(temp_name), Path(temp_name)

        if source_type == "s3_object":
            raise UnsupportedSourceReferenceError(
                "Direct s3_object sources are not implemented yet. Use a presigned_url."
            )

        raise UnsupportedSourceReferenceError(f"Unsupported source reference type: {source_type}")

    @staticmethod
    def _write_segments_json(output_dir: Path, segments: list[SegmentRecord]) -> Path:
        path = output_dir / "segments.json"
        payload = [
            {
                "startSeconds": round(segment.start_seconds, 3),
                "endSeconds": round(segment.end_seconds, 3),
                "text": segment.text,
            }
            for segment in segments
        ]
        path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return path

    @staticmethod
    def _write_vtt(output_dir: Path, segments: list[SegmentRecord]) -> Path:
        path = output_dir / "captions.vtt"
        lines = ["WEBVTT", ""]
        for segment in segments:
            lines.append(
                f"{TranscriptionService._format_vtt_timestamp(segment.start_seconds)} --> "
                f"{TranscriptionService._format_vtt_timestamp(segment.end_seconds)}"
            )
            lines.append(segment.text)
            lines.append("")
        path.write_text("\n".join(lines), encoding="utf-8")
        return path

    @staticmethod
    def _write_srt(output_dir: Path, segments: list[SegmentRecord]) -> Path:
        path = output_dir / "captions.srt"
        lines: list[str] = []
        for index, segment in enumerate(segments, start=1):
            lines.append(str(index))
            lines.append(
                f"{TranscriptionService._format_srt_timestamp(segment.start_seconds)} --> "
                f"{TranscriptionService._format_srt_timestamp(segment.end_seconds)}"
            )
            lines.append(segment.text)
            lines.append("")
        path.write_text("\n".join(lines), encoding="utf-8")
        return path

    @staticmethod
    def _format_vtt_timestamp(total_seconds: float) -> str:
        hours = int(total_seconds // 3600)
        minutes = int((total_seconds % 3600) // 60)
        seconds = int(total_seconds % 60)
        milliseconds = int(round((total_seconds - int(total_seconds)) * 1000))
        return f"{hours:02}:{minutes:02}:{seconds:02}.{milliseconds:03}"

    @staticmethod
    def _format_srt_timestamp(total_seconds: float) -> str:
        hours = int(total_seconds // 3600)
        minutes = int((total_seconds % 3600) // 60)
        seconds = int(total_seconds % 60)
        milliseconds = int(round((total_seconds - int(total_seconds)) * 1000))
        return f"{hours:02}:{minutes:02}:{seconds:02},{milliseconds:03}"
