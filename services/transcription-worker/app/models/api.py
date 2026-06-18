from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, Field, HttpUrl


SourceReferenceType = Literal["local_path", "url", "presigned_url", "s3_object"]


class ArtifactReferenceDto(BaseModel):
    kind: Literal["local_path"] = "local_path"
    path: str


class SourceReferenceDto(BaseModel):
    type: SourceReferenceType
    value: str


class TranscriptionOptionsDto(BaseModel):
    model: str | None = None
    device: str | None = None
    computeType: str | None = None
    beamSize: int | None = Field(default=None, ge=1, le=20)
    enableVad: bool | None = None
    enableWordTimestamps: bool | None = None


class CallbackOptionsDto(BaseModel):
    url: HttpUrl
    token: str | None = None


class TranscriptionJobCreateRequest(BaseModel):
    correlationId: str = Field(min_length=1, max_length=200)
    videoId: str = Field(min_length=1, max_length=200)
    sourceReference: SourceReferenceDto
    language: str | None = Field(default=None, max_length=20)
    outputFormats: list[Literal["vtt", "srt"]] = Field(default_factory=lambda: ["vtt", "srt"])
    options: TranscriptionOptionsDto | None = None
    callback: CallbackOptionsDto


class JobAcceptedResponse(BaseModel):
    jobId: str
    status: str


class JobStatusResponse(BaseModel):
    jobId: str
    correlationId: str
    videoId: str
    status: str
    progressPercent: int
    stage: str
    message: str | None = None
    language: str | None = None
    startedAt: str | None = None
    completedAt: str | None = None
    artifacts: list[ArtifactReferenceDto] = Field(default_factory=list)
    failureReason: str | None = None


class HealthResponse(BaseModel):
    status: str
    outputRoot: str
    sharedMediaRoot: str | None = None
