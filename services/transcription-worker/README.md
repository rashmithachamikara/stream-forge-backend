# Transcription Worker

Internal Python worker for Stream Forge local transcription jobs.

## What It Does

- accepts transcription jobs from Stream Forge
- resolves local-path or URL-based media references
- runs `faster-whisper`
- writes staged `captions.vtt`, `captions.srt`, and `segments.json`
- exposes job status/progress
- calls back Stream Forge when work finishes or fails

## Environment

Copy `.env.example` into your local environment setup and adjust the paths/secrets for your machine or container.

- `STREAMFORGE_LOCAL_STORAGE_ROOT`
  - optional local storage root override for both uploads and transcription output; when empty, the worker computes a repo-root-anchored default under `data`
- `TRANSCRIPTION_WORKER_OUTPUT_ROOT`
  - optional override for the transcription output root; defaults to `<shared-root>/transcription-output`
- `TRANSCRIPTION_WORKER_SHARED_MEDIA_ROOT`
  - optional override for the shared uploads/media root; defaults to `<shared-root>/uploads`
- `TRANSCRIPTION_WORKER_CALLBACK_TIMEOUT_SECONDS`
  - default `30`
- `TRANSCRIPTION_WORKER_CALLBACK_AUTH_HEADER`
  - default `X-StreamForge-Worker-Secret`
- `TRANSCRIPTION_WORKER_CALLBACK_SECRET`
  - optional callback secret value
- `TRANSCRIPTION_WORKER_DEFAULT_MODEL`
  - default `small`
- `TRANSCRIPTION_WORKER_DEFAULT_DEVICE`
  - default `cpu`
- `TRANSCRIPTION_WORKER_DEFAULT_COMPUTE_TYPE`
  - default `int8`
- `TRANSCRIPTION_WORKER_DEFAULT_BEAM_SIZE`
  - default `5`
- `TRANSCRIPTION_WORKER_ENABLE_VAD`
  - default `true`
- `TRANSCRIPTION_WORKER_WORD_TIMESTAMPS`
  - default `false`

## Run

```bash
python -m uvicorn app.main:app --host 0.0.0.0 --port 8090
```

## API

- `GET /health`
- `POST /jobs/transcriptions`
- `GET /jobs/transcriptions/{jobId}`
- `GET /jobs/transcriptions/{jobId}/result`

## Notes

- local filesystem paths and HTTP/HTTPS URLs are supported now
- raw S3 bucket/key reads are left as a future storage adapter; presigned URLs work today
- with no shared-root env overrides, the worker derives defaults from the repo root so clone-and-run local setups stay aligned with the `.NET` app
- older `STREAMFORGE_SHARED_ROOT` and `TRANSCRIPTION_WORKER_SHARED_ROOT` env names still work as fallback aliases
