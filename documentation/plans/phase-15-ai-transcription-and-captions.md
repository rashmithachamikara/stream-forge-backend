# Phase 15 - AI Transcription and Captions

Status: [X] Complete

## Current Progress Snapshot

Implemented already:

- Python transcription worker scaffold under `services/transcription-worker/`
- internal worker endpoints for submit, status, health, and result retrieval
- local `faster-whisper` execution path
- staged artifact generation for `VTT`, `SRT`, and `segments.json`
- callback client from Python back to Stream Forge
- configurable shared local storage roots for uploads/transcription output
- source-reference handling that can support local paths, S3-style references, and presigned URLs
- worker Dockerfile, README, `.env.example`, and manual smoke-test script

Phase 15 completion scope:

- backend transcription orchestration
- caption persistence and delivery
- transcript chunk persistence
- keyword transcript search
- admin-managed backend transcription settings
- broader backend automated tests for transcription orchestration, storage, authorization, and failure handling

Deferred beyond Phase 15:

- PostgreSQL full-text search, embeddings, semantic retrieval, and grounded Q&A in `documentation/plans/phase-17-transcript-intelligence-and-rag.md`
- admin/configuration UI work, which is intentionally out of scope for this backend execution plan

Implemented in the latest backend pass:

- `.NET` transcription provider abstraction and orchestration
- queueing transcription automatically after video readiness
- callback ingestion and canonical persistence into `VideoTranscriptions`
- callback ingestion of `segments.json` into persisted `VideoTranscriptChunks`
- durable worker metadata persistence for correlation IDs, worker job IDs, model, and failure reason
- backend transcription status/read APIs with worker-polled live status for running jobs
- player-facing caption retrieval APIs
- per-video transcript keyword search API over persisted chunks
- admin-managed transcription settings persisted through a `SystemSettings` key-value store
- a dedicated frontend contract doc for transcript UI, polling, search-result behavior, and multi-artifact handling in `documentation/TRANSCRIPTIONS_FRONTEND_CONTRACT.md`

## Purpose

Automatically transcribe uploaded videos and turn those transcripts into usable caption assets and transcript outputs.

The initial target is Whisper-based transcription triggered after a video upload has completed and the source media is ready. This phase should keep the transcription provider behind an application abstraction so the system is not tightly coupled to one local model runtime or one hosted AI vendor.

This phase should lay the backend foundation for later transcript intelligence work without implementing the RAG layer itself.

## Why This Fits The Current Architecture

The repo already has:

- `VideoTranscription` domain entity
- `TranscriptionStatus` enum
- `ProcessingJobType.Transcription`
- Hangfire-based background processing
- existing media storage and authorization patterns

That means transcription should be implemented as another asynchronous processing capability, not as request-time work.

## Recommended Architecture

Use a hybrid design:

- `.NET API + Application` remain the source of truth for product workflows and persistence
- Hangfire remains the orchestrator on the Stream Forge side
- a separate Python worker handles local `faster-whisper` execution
- future hosted transcription providers plug into the same .NET provider abstraction

The key boundary is:

- `.NET` owns product state, permissions, APIs, and stored metadata
- Python owns local AI execution

Do not make the Python worker the owner of application tables or public product APIs.

## 1. Trigger Point

Do not transcribe during the upload HTTP request itself.

Preferred flow:

1. upload completes
2. video processing finishes and the source/original file is known
3. transcription job is queued from `.NET`
4. `.NET` submits work to the configured transcription provider
5. provider returns transcript/caption outputs
6. `VideoTranscriptions` and related search artifacts are updated

This keeps large model inference out of the request path and avoids mixing upload reliability with AI runtime cost.

## 2. Provider Shape

Add Application abstractions such as:

- `ITranscriptionProvider`
- optional `ITranscriptSearchProvider` later if transcript retrieval grows beyond the first implementation

Suggested transcription-provider responsibilities:

- accept a source media reference
- accept requested output formats
- accept optional language hint
- accept model/runtime options
- return generated transcript artifacts, structured segment data, and metadata

This keeps controllers and use cases decoupled from:

- local Python `faster-whisper`
- future hosted providers
- future provider-specific auth/secrets

## 3. First Provider Strategy

### Local Provider

The first local provider should be:

- `.NET` transcription adapter
- internal Python worker using `faster-whisper`

The worker should live under:

- `services/transcription-worker/`

The Python worker should:

- expose an internal control API
- accept job submission requests
- process jobs in the background
- expose job-status polling
- call back `.NET` when work completes or fails

### Hosted Providers Later

The same `.NET` abstraction should later support hosted providers such as:

- OpenAI-compatible transcription APIs
- AssemblyAI
- other vendor transcription services

Those hosted providers should not require changes to the application use-case flow, only new provider adapters and configuration.

## 4. Hangfire And Python Handoff

Hangfire should stay in charge of the Stream Forge job lifecycle.

Recommended target flow:

1. Hangfire job starts in `.NET`
2. `.NET` resolves video/storage metadata and active transcription settings
3. `.NET` calls Python worker `POST /jobs/transcriptions`
4. Python returns a worker `jobId`
5. Python processes the job in the background
6. `.NET` can poll status if needed
7. Python calls a `.NET` callback endpoint on completion/failure
8. `.NET` persists canonical transcript records and final state

Recommended completion strategy:

- callback is the primary success/failure notification path
- polling is the fallback/reconciliation path

This avoids a long synchronous HTTP call while still keeping Hangfire as the orchestrator.

### Planned End-To-End Flow

Current implementation status:

- Steps 7, 8, 10, 11, 12, 13, 14, and the worker side of 15 exist in the Python worker today.
- The `.NET` orchestration, persistence, and public API pieces around those steps are still pending.

The current planned pipeline is:

1. user uploads video
2. Stream Forge completes upload and processing
3. video becomes ready
4. `.NET` checks whether auto-transcription is enabled
5. Hangfire starts the transcription orchestration job
6. `.NET` resolves the active transcription provider and settings
7. `.NET` submits a transcription job to the Python worker
8. Python returns a worker `jobId`
9. `.NET` stores that worker `jobId` in transcription job state for polling, reconciliation, and callback matching
10. Python reads source media from shared storage/path
11. Python runs `faster-whisper`
12. Python generates timestamped transcript segments
13. Python derives `VTT` and `SRT` outputs
14. Python writes transcript artifacts to a shared staging/output location
15. Python calls back `.NET` with completion metadata and output references
16. `.NET` ingests the caption artifacts into Stream Forge canonical storage
17. `.NET` ingests staged transcript segment JSON
18. `.NET` creates or updates `VideoTranscriptions`
19. `.NET` persists normalized transcript chunks in the database
20. keyword transcript search becomes available over persisted transcript chunks
21. later phases can add embedding generation, semantic retrieval, and grounded Q&A on top of those chunks

Operationally:

- callback is the primary completion signal
- polling is the fallback if callback delivery fails
- `.NET` remains the source of truth for canonical product state
- Python remains the execution worker for local AI tasks
- live progress can be fetched on demand from the Python worker status endpoint without frequent database writes
- admin and owner-facing status views should prefer worker-polled live progress for running transcription jobs and fall back to persisted coarse state when the worker is unavailable

## 5. Media Access Strategy

The Python worker should usually process media by storage reference rather than by receiving large uploaded video bytes through HTTP.

Preferred approaches:

- shared local mount/path for self-hosted local storage
- S3/object-storage bucket+key or presigned URL for remote storage
- provider-specific storage reference models later if needed

Avoid using the Python control API as a giant file-transfer channel for media files.

Local filesystem paths should be treated as only one kind of storage reference, not as the universal contract.

## 6. Configuration

Add a new configuration section such as `Transcription`.

Current status:

- the worker already has its own local runtime configuration and `.env.example`
- Stream Forge `.NET` now supports effective transcription settings from defaults plus persisted `SystemSettings`
- admin-facing settings APIs are in place; the admin UI still remains to be built

Suggested options:

- `Enabled`
- `AutoTranscribeOnReady`
- `Provider`
- `Language`
- `OutputFormats`
- `MaxConcurrentJobs`
- `WorkerBaseUrl`
- `WorkerCallbackSecret`
- `PollIntervalSeconds`
- `JobTimeoutMinutes`

For the local Python `faster-whisper` worker, model/runtime options should be represented cleanly and kept provider-specific, for example:

- provider type = `local-faster-whisper`
- provider settings include:
  - model
  - device (`cpu`, `cuda`)
  - compute type
  - beam size
  - word timestamps
  - VAD settings

Suggested conservative defaults:

- `Enabled = false`
- `AutoTranscribeOnReady = false`
- `Provider = local-faster-whisper`
- `Language = auto`
- `OutputFormats = [ "vtt", "srt" ]`

Keep this off by default because model downloads, CPU load, GPU usage, and storage growth are deployment concerns.

## 7. Settings Direction

Phase 15 now has backend-managed transcription settings persisted in `SystemSettings`.

For backend planning, the design should support:

- provider selection
- local vs hosted provider choice
- model selection
- optional language hints
- enable/disable toggles
- secure storage of hosted-provider secrets

The likely future data shape is:

- provider-specific integration records for configured providers
- system-level settings for active provider selection and feature toggles

UI exposure of these settings is intentionally outside the scope of this backend execution phase.

## 8. Storage Model

Use `VideoTranscriptions` as the metadata record and store generated files in the existing storage system.

Recommended stored artifacts:

- `captions.vtt`
- `captions.srt`
- `segments.json`

Recommended storage path pattern:

- `videos/{videoId}/transcriptions/{language}/captions.vtt`
- `videos/{videoId}/transcriptions/{language}/captions.srt`
- `videos/{videoId}/transcriptions/{language}/segments.json`

Each `VideoTranscription` row should track:

- `VideoId`
- `Language`
- `Format`
- `StoragePath`
- `Status`
- `Source` or provider type
- provider/model metadata where useful
- timestamps

For the first iteration, one row per output format is fine and fits the current schema well.

Structured transcript text should cross from Python to `.NET` as staged JSON segment data, for example:

- `startSeconds`
- `endSeconds`
- `text`

That segment data should be written by the Python worker as `segments.json` and then ingested by `.NET`.

That staged JSON is the preferred source for normalized transcript chunk persistence.

`VTT` and `SRT` remain the stored caption artifacts for playback and download.

Artifact references should not be limited to local file paths. Depending on deployment, they may be:

- shared local paths
- S3 bucket/key references
- presigned URLs
- other provider-specific storage references

### Planned Schema Changes

Before the schema change, it is necessary to add a new schema version indocumentation\schema\

The expected schema work for this phase is:

1. expand `VideoTranscriptions`
2. add transcript-chunk persistence
3. defer semantic-retrieval schema additions to the later transcript-intelligence phase

#### `VideoTranscriptions`

Keep subtitle and caption artifact paths here.

Each row should continue to represent one stored transcription/caption artifact, such as:

- one `vtt` output
- one `srt` output

Planned fields and metadata include:

- `VideoId`
- `Language`
- `Format`
- `StoragePath`
- `Status`
- `CorrelationId`
- `WorkerJobId`
- `Provider`
- `Model`
- `FailureReason`
- failure/error details
- timestamps such as created/completed/updated

`SourcePath` does not need to be stored again here because the transcription input can be resolved from the existing video/file model.

#### `VideoTranscriptChunks`

Add a transcript chunk table for searchable normalized transcript content.

Suggested fields:

- `Id`
- `VideoId`
- `TranscriptionId`
- `Language`
- `StartSeconds`
- `EndSeconds`
- `Content`
- `CreatedAt`
- `UpdatedAt`

#### Indexing

Add indexes for:

- `IX_VideoTranscriptions_VideoId`
- `IX_VideoTranscriptions_Status`
- `IX_VideoTranscriptions_WorkerJobId`
- `IX_VideoTranscriptChunks_VideoId`
- `IX_VideoTranscriptChunks_TranscriptionId`
- `IX_VideoTranscriptChunks_VideoId_StartSeconds`

Also plan later for:

- PostgreSQL full-text indexing on chunk content
- `pgvector` indexing for embedding similarity search

#### Progress State

For transcription progress, keep only durable coarse state in the database, such as:

- queued
- running
- completed
- failed
- worker job id

Live `progressPercent` should come from the Python worker status endpoint on demand rather than from frequent database writes.

During the long-running transcribing stage, that live progress should be derived from actual media progress rather than from coarse stage milestones alone. Once source media duration is known, the worker should calculate the main transcription percentage from `latestSegmentEndSeconds / mediaDurationSeconds` and map that into the transcribing slice of the job.

A practical default split is:

- `0-5%`: source resolution and job setup
- `5-95%`: active transcription derived from transcribed time versus total duration
- `95-100%`: artifact generation and callback delivery

That gives the admin UI a meaningful in-flight percentage without turning the database into a high-frequency progress log.

## 9. Transcript Search Foundation

Transcription should not stop at caption files. To support later search and question-answering work, the system should also derive searchable transcript chunks.

Recommended model:

- split transcripts into timestamped chunks
- store chunk text plus metadata:
  - `VideoId`
  - `Language`
  - `StartSeconds`
  - `EndSeconds`
  - chunk text

Phase 15 delivers:

- transcript chunks stored as regular relational rows
- keyword search over those persisted chunks
- stable chunk metadata that later retrieval stages can build on

PostgreSQL full-text search, semantic retrieval, embeddings, and grounded Q&A are deferred to Phase 17.

## 10. API Surface

Add endpoints for:

- list transcriptions for a video
- get/download a specific transcription file
- optionally request transcription for a video manually
- optionally re-run transcription for admins/editors
- search within a video transcript
Suggested examples:

- `GET /api/v1/videos/{videoId}/transcriptions`
- `GET /api/v1/videos/{videoId}/transcriptions/{transcriptionId}`
- `GET /api/v1/videos/{videoId}/transcriptions/{transcriptionId}/chunks`
- `POST /api/v1/videos/{videoId}/transcriptions`
- `POST /api/v1/videos/{videoId}/transcriptions/{transcriptionId}/retry`
- `GET /api/v1/videos/{videoId}/transcript-search?q=...`

Internal endpoints may also be needed for the Python worker callback, for example:

- `POST /internal/transcriptions/callback`

Playback-related caption access should reuse existing video authorization rules so private videos do not expose captions publicly.

## 11. Failure And Operational Considerations

Whisper-based transcription can be expensive in CPU, RAM, disk, GPU, and time.

Plan for:

- model installation/download strategy
- CPU-only vs GPU deployments
- queue concurrency limits
- timeout handling
- worker health checks
- callback authentication
- reconciliation when callback fails
- large-file runtime limits
- language auto-detection inaccuracies
- reprocessing strategy when a model changes
- later embedding generation cost and storage size
- later search-index refresh behavior after transcript updates

Do not block upload success on transcription success.
Transcription should be an asynchronous enhancement, not part of the critical upload contract.

## 12. Testing

Add tests for:

- transcription auto-queue orchestration after video readiness
- disabled-feature behavior
- successful provider handoff and callback completion
- failure transitions to `Failed`
- authorization for transcript/caption retrieval
- storage path generation
- per-format record handling
- transcript chunk generation
- keyword transcript search behavior

Use a fake transcription provider for most `.NET` tests.
Python worker smoke tests can be separate and lighter-weight.

## Acceptance Criteria

- A ready video can automatically queue a transcription job when enabled.
- `.NET` can hand work to a local Python `faster-whisper` worker through an internal provider adapter.
- Generated caption files are stored and linked through `VideoTranscriptions`.
- Transcription status is queryable through the API, with live worker status available while jobs are running.
- Caption retrieval respects existing video authorization rules.
- Transcript search can return timestamped matches for a video.
- The feature is configurable and off by default.
- Tests cover orchestration, authorization, and failure handling.

## Notes

- Keep project-level implementation tracking in `EXECUTION_PLAN.md`.
- Keep Python-worker-specific implementation details in `services/transcription-worker/EXECUTION_PLAN.md`.
- Start with one local provider/runtime and keep the abstraction clean enough to support future hosted alternatives.
- `VTT` should be the primary player-facing output format, even if `SRT` and plain text are also stored.
