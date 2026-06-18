from __future__ import annotations

import asyncio
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException, status

from app.config import WorkerSettings
from app.models.api import (
    HealthResponse,
    JobAcceptedResponse,
    JobStatusResponse,
    TranscriptionJobCreateRequest,
)
from app.models.jobs import ArtifactRecord, WorkerJob
from app.services.callback_client import CallbackClient, CallbackDeliveryError
from app.services.job_store import JobStore
from app.services.transcription_service import (
    TranscriptionService,
    UnsupportedSourceReferenceError,
)


logger = logging.getLogger("streamforge.transcription_worker")

settings = WorkerSettings.load()
job_store = JobStore()
callback_client = CallbackClient(settings)
transcription_service = TranscriptionService(settings)


@asynccontextmanager
async def lifespan(_: FastAPI):
    settings.output_root.mkdir(parents=True, exist_ok=True)
    yield


app = FastAPI(title="Stream Forge Transcription Worker", lifespan=lifespan)


@app.get("/health", response_model=HealthResponse)
async def get_health() -> HealthResponse:
    return HealthResponse(
        status="ok",
        sharedRoot=str(settings.shared_root),
        outputRoot=str(settings.output_root),
        sharedMediaRoot=str(settings.shared_media_root),
    )


@app.post(
    "/jobs/transcriptions",
    response_model=JobAcceptedResponse,
    status_code=status.HTTP_202_ACCEPTED,
)
async def create_transcription_job(
    request: TranscriptionJobCreateRequest,
) -> JobAcceptedResponse:
    job = WorkerJob(
        correlation_id=request.correlationId,
        video_id=request.videoId,
        source_reference_type=request.sourceReference.type,
        source_reference_value=request.sourceReference.value,
        callback_url=str(request.callback.url),
        callback_token=request.callback.token,
        requested_formats=request.outputFormats,
        language=request.language,
        options={
            "model": request.options.model if request.options and request.options.model else settings.default_model,
            "device": request.options.device if request.options and request.options.device else settings.default_device,
            "compute_type": (
                request.options.computeType
                if request.options and request.options.computeType
                else settings.default_compute_type
            ),
            "beam_size": (
                request.options.beamSize
                if request.options and request.options.beamSize
                else settings.default_beam_size
            ),
            "enable_vad": (
                request.options.enableVad
                if request.options and request.options.enableVad is not None
                else settings.enable_vad
            ),
            "enable_word_timestamps": (
                request.options.enableWordTimestamps
                if request.options and request.options.enableWordTimestamps is not None
                else settings.enable_word_timestamps
            ),
        },
    )

    job_store.add(job)
    asyncio.create_task(_run_job(job.id))

    return JobAcceptedResponse(jobId=job.id, status="accepted")


@app.get("/jobs/transcriptions/{job_id}", response_model=JobStatusResponse)
async def get_transcription_job(job_id: str) -> JobStatusResponse:
    job = job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Job not found.")

    return JobStatusResponse(**job.to_status_dict())


@app.get("/jobs/transcriptions/{job_id}/result")
async def get_transcription_result(job_id: str) -> dict:
    job = job_store.get(job_id)
    if job is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Job not found.")
    if job.status != "completed":
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail="Job has not completed yet.",
        )

    return {
        "jobId": job.id,
        "artifacts": [
            {"kind": artifact.kind, "path": artifact.path}
            for artifact in job.artifact_records
        ],
        "segmentsFilePath": job.segments_file_path,
        "language": job.detected_language or job.language,
    }


async def _run_job(job_id: str) -> None:
    job = job_store.get(job_id)
    if job is None:
        return

    try:
        job.mark_running("resolving_source", 10, "Resolving source reference.")
        await asyncio.sleep(0)

        job.mark_running("transcribing", 35, "Running faster-whisper transcription.")
        result = await asyncio.to_thread(transcription_service.transcribe, job)

        artifacts = [
            ArtifactRecord(kind="local_path", path=str(path.resolve()))
            for path in result.artifact_paths.values()
        ]

        job.mark_running("generating_artifacts", 85, "Preparing artifact references.")
        await asyncio.sleep(0)

        segments_path = result.artifact_paths.get("segments.json")
        job.mark_running("delivering_callback", 95, "Sending completion callback.")
        job.mark_completed(
            detected_language=result.detected_language,
            artifacts=artifacts,
            segments_file_path=str(segments_path.resolve()) if segments_path else None,
        )
        await asyncio.to_thread(callback_client.send_completion, job)
    except (FileNotFoundError, UnsupportedSourceReferenceError) as exc:
        logger.exception("Transcription job %s failed during source resolution.", job.id)
        job.mark_failed(str(exc))
        await _try_send_failure_callback(job)
    except CallbackDeliveryError as exc:
        logger.exception("Transcription job %s completed but callback failed.", job.id)
        job.failure_reason = str(exc)
    except Exception as exc:  # pragma: no cover - defensive boundary
        logger.exception("Unexpected transcription worker failure for job %s.", job.id)
        job.mark_failed(str(exc))
        await _try_send_failure_callback(job)


async def _try_send_failure_callback(job: WorkerJob) -> None:
    try:
        await asyncio.to_thread(callback_client.send_completion, job)
    except Exception:
        logger.exception("Failure callback could not be delivered for job %s.", job.id)
