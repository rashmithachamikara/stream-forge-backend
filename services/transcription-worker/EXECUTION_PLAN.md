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
- generate `VTT`, `SRT`, and optional plain-text transcript outputs
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
- `sourcePath` or storage reference
- `language`
- `model`
- `outputFormats`
- optional provider/runtime options
- callback information or callback token

Suggested response:

- worker `jobId`
- `status = accepted`

### Job Status

- `GET /jobs/transcriptions/{jobId}`

Suggested response fields:

- `jobId`
- `status`
- `progressPercent` if available
- `message`
- `startedAt`
- `completedAt`

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
- transcript segments or generated artifact references
- failure details if any
- model/provider metadata

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

## Media Handling

Preferred approach:

- read media directly from a shared local path or other storage reference

Avoid:

- uploading large video bytes through the control API

The worker should be able to:

- verify source path existence
- read the media file
- write temporary/generated transcript artifacts

## Output Handling

The worker should generate:

- `VTT`
- `SRT`
- optional plain text

The worker can either:

1. return generated text directly in the callback payload for smaller outputs, or
2. write artifacts to a staging/output path and include references in the callback

For Stream Forge, the cleaner long-term design is:

- worker generates artifacts
- Stream Forge persists final artifacts through its own storage/persistence rules

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

- [ ] Create Python project scaffold under `services/transcription-worker/`
- [ ] Add `requirements.txt`
- [ ] Add FastAPI entry point
- [ ] Add worker job models and status tracking
- [ ] Add transcription submission endpoint
- [ ] Add job-status endpoint
- [ ] Add health endpoint
- [ ] Add `faster-whisper` integration service
- [ ] Add `SRT` and `VTT` generation helpers
- [ ] Add callback client back to Stream Forge
- [ ] Add local configuration model
- [ ] Add Dockerfile
- [ ] Add worker README with install/run instructions
- [ ] Add smoke test script or test path

## Notes

- Stream Forge `.NET` remains the source of truth for canonical application state.
- This worker should stay focused on local AI execution and job coordination.
- If a future broker-based architecture replaces the current handoff model, this worker can evolve without forcing a redesign of transcript storage or public APIs.
