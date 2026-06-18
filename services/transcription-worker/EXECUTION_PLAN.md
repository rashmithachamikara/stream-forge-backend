# Transcription Worker - Local Execution Plan

## Purpose

This plan covers the Python worker that runs local transcription jobs for Stream Forge.

This worker is not the public product API and should not become the owner of application persistence rules. Its job is to:

- accept internal transcription jobs from Stream Forge
- execute local model inference
- track worker-side job status
- return results back to Stream Forge

## Responsibilities

- expose an internal HTTP API for job submission and status
- run `faster-whisper` transcription jobs in the background
- generate `VTT`, `SRT`, and staged `segments.json` transcript output
- report progress/state for pending, running, completed, and failed jobs
- call back the Stream Forge backend when jobs complete or fail
- support local model/runtime configuration

## Non-Responsibilities

- public user-facing APIs
- direct ownership of Stream Forge domain tables
- authorization logic for video access
- long-term product state as the canonical source of truth

## Suggested Project Shape

```text
services/transcription-worker/
  EXECUTION_PLAN.md
  README.md
  requirements.txt
  Dockerfile
  app/
    main.py
    api/
    models/
    services/
    workers/
    adapters/
```

## Runtime Architecture

Recommended first implementation:

- FastAPI for the internal control API
- in-process background execution or a dedicated local worker loop
- `faster-whisper` for transcription
- local filesystem/shared mount access for media files

Possible later evolution:

- separate worker process
- broker-backed execution
- hosted-model adapters

## Internal API Contract

### Submit Job

- `POST /jobs/transcriptions`

Suggested request fields:

- `jobId` or correlation id from Stream Forge
- `videoId`
- `sourceReference`
- `language`
- `model`
- `outputFormats`
- optional provider/runtime options
- callback information or callback token

`sourceReference` should support more than local paths, for example:

- shared local file path
- S3 bucket/key reference
- presigned URL

Suggested response:

- worker `jobId`
- `status = accepted`

### Job Status

- `GET /jobs/transcriptions/{jobId}`

Suggested response fields:

- `jobId`
- `status`
- `progressPercent` if available
- current stage/message such as model loading, transcribing, artifact generation, callback delivery
- `message`
- `startedAt`
- `completedAt`

This status endpoint is the primary source for live worker progress. Stream Forge can poll it on demand instead of expecting frequent progress persistence in the application database.

### Health

- `GET /health`

Suggested checks:

- service ready
- model/runtime available
- callback target configuration present if required

## Callback Contract

The worker should call back Stream Forge on completion or failure.

Suggested behavior:

- callback is the primary completion path
- polling remains the fallback

Suggested callback payload:

- Stream Forge correlation/job id
- worker job id
- status
- language
- output artifacts
- generated artifact references, including `segments.json`
- failure details if any
- model/provider metadata

The callback should stay small and should not stream large transcript bodies inline. It should primarily signal completion/failure and point Stream Forge to staged outputs.

Callback auth should be simple and explicit, such as:

- shared secret header
- signed internal token

## Local Model Support

Start with:

- `tiny`
- `tiny.en`
- `base`
- `base.en`
- `small`
- `small.en`
- `medium`
- `medium.en`
- optional `large-v3`

Worker config should support:

- model name
- device (`cpu`, `cuda`)
- compute type
- beam size
- VAD toggle
- word timestamps toggle
- shared media input root
- staged artifact output root
- optional object-storage access configuration when local shared paths are not used

## Media Handling

Preferred approach:

- read media directly from a storage reference

Avoid:

- uploading large video bytes through the control API

The worker should be able to:

- resolve supported source references
- read the media file
- write temporary/generated transcript artifacts

For local deployments, shared filesystem roots should be configurable rather than hardcoded so local runs, Docker volumes, and future deployment layouts can all use the same worker design.

For object-storage-backed deployments, the worker should be able to read media from S3-style references or presigned URLs instead of relying on a shared local mount.

## Output Handling

The worker should generate:

- `VTT`
- `SRT`
- `segments.json` for Stream Forge chunking/search persistence

`segments.json` should contain normalized transcript segment data such as:

- `startSeconds`
- `endSeconds`
- `text`

Recommended split:

1. write `VTT`, `SRT`, and `segments.json` artifacts to a staging/output path
2. return artifact references in the callback

Suggested artifact layout:

- `{TRANSCRIPTION_OUTPUT_ROOT}/{jobId}/captions.vtt`
- `{TRANSCRIPTION_OUTPUT_ROOT}/{jobId}/captions.srt`
- `{TRANSCRIPTION_OUTPUT_ROOT}/{jobId}/segments.json`

The staging/output root should be configurable and should typically be backed by a shared directory or Docker volume visible to both the Python worker and Stream Forge `.NET`.

For remote/object-storage deployments, artifact references may instead point to:

- S3 bucket/key locations
- presigned URLs
- other provider-specific output references

For Stream Forge, the cleaner long-term design is:

- worker generates artifacts
- Stream Forge persists final artifacts through its own storage/persistence rules
- Stream Forge ingests `segments.json` to persist transcript chunks for search and embeddings

## Error Handling

The worker should handle:

- missing media path
- unsupported model/runtime configuration
- model load failures
- transcription runtime failures
- callback delivery failure

Recommended behavior:

- mark worker job failed with explicit error details
- allow `.NET` to reconcile by polling if callback fails

## Security

This service should be internal-only.

Recommended protections:

- bind to internal network only
- use callback authentication
- validate request shape strictly
- avoid exposing local file-system details unnecessarily

## Implementation Steps

- [X] Create Python project scaffold under `services/transcription-worker/`
- [X] Add `requirements.txt`
- [X] Add FastAPI entry point
- [X] Add worker job models and status tracking
- [X] Add transcription submission endpoint
- [X] Add job-status endpoint
- [X] Add stage/progress reporting for polling
- [X] Add health endpoint
- [X] Add `faster-whisper` integration service
- [X] Add `SRT` and `VTT` generation helpers
- [X] Add callback client back to Stream Forge
- [X] Add local configuration model
- [X] Add configurable shared media root and staged artifact output root
- [X] Add storage-reference handling for local paths and S3-style inputs
- [X] Add Dockerfile
- [X] Add worker README with install/run instructions
- [ ] Add smoke test script or test path

## Notes

- Stream Forge `.NET` remains the source of truth for canonical application state.
- This worker should stay focused on local AI execution and job coordination.
- Live progress should be exposed from the worker on demand rather than treated as a high-frequency persisted database field.
- If a future broker-based architecture replaces the current handoff model, this worker can evolve without forcing a redesign of transcript storage or public APIs.
