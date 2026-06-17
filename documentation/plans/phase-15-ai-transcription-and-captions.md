# Phase 15 - AI Transcription and Captions

Status: [ ] Planned

## Purpose

Automatically transcribe uploaded videos and turn those transcripts into usable caption assets and transcript outputs.

The initial target is Whisper-based transcription triggered after a video upload has completed and the source media is ready. This phase should keep the transcription provider behind an application abstraction so the system is not tightly coupled to one local model runtime or one hosted AI vendor.

This phase should also lay the foundation for transcript-based search and user Q&A over videos.

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
- return generated transcript artifacts plus metadata

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

Recommended flow:

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
17. `.NET` creates or updates `VideoTranscriptions`
18. `.NET` persists normalized transcript chunks in the database
19. embedding generation runs as the next stage
20. the configured embedding provider generates vectors for transcript chunks
21. `.NET` stores chunk embeddings
22. transcript search becomes available
23. video Q&A retrieves matching chunks
24. answer generation uses retrieved chunks with cited timestamps

Operationally:

- callback is the primary completion signal
- polling is the fallback if callback delivery fails
- `.NET` remains the source of truth for canonical product state
- Python remains the execution worker for local AI tasks

## 5. Media Access Strategy

The Python worker should usually process media by storage path or shared-storage reference rather than by receiving large uploaded video bytes through HTTP.

Preferred approaches:

- shared local mount/path for self-hosted local storage
- object-storage URL or provider reference for remote storage later

Avoid using the Python control API as a giant file-transfer channel for media files.

## 6. Configuration

Add a new configuration section such as `Transcription`.

Suggested options:

- `Enabled`
- `AutoTranscribeOnReady`
- `Provider`
- `Language`
- `OutputFormats`
- `MaxConcurrentJobs`
- `StorePlainTextTranscript`
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

## 7. Settings And Admin UX Direction

Phase 15 should assume that transcription settings may later be managed from an admin UI.

That means the design should support:

- provider selection
- local vs hosted provider choice
- model selection
- optional language hints
- enable/disable toggles
- secure storage of hosted-provider secrets

The frontend should never hold hosted provider API keys for runtime transcription calls.

The likely future data shape is:

- provider-specific integration records for configured providers
- system-level settings for active provider selection and feature toggles

This phase does not need to finalize that schema, but it should stay compatible with it.

## 8. Storage Model

Use `VideoTranscriptions` as the metadata record and store generated files in the existing storage system.

Recommended stored artifacts:

- `captions.vtt`
- `captions.srt`
- optional `transcript.txt`

Recommended storage path pattern:

- `videos/{videoId}/transcriptions/{language}/captions.vtt`
- `videos/{videoId}/transcriptions/{language}/captions.srt`
- `videos/{videoId}/transcriptions/{language}/transcript.txt`

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

## 9. Transcript Search And Q&A Model

Transcription should not stop at caption files. To support search and user questions about videos, the system should also derive searchable transcript chunks.

Recommended model:

- split transcripts into timestamped chunks
- store chunk text plus metadata:
  - `VideoId`
  - `Language`
  - `StartSeconds`
  - `EndSeconds`
  - chunk text
  - optional embedding vector
- support both:
  - full-text search for keyword matching
  - semantic/vector retrieval for natural-language questions

The plan is to use PostgreSQL full-text search plus `pgvector` for semantic retrieval.

That means:

- transcript chunks should be stored as regular relational rows
- chunk text should support full-text indexing for keyword search
- embedding vectors should be stored in PostgreSQL using `pgvector`
- semantic retrieval should use vector similarity over those stored chunk embeddings

### Recommended Retrieval Strategy

Start with a focused transcript-based retrieval approach rather than a large, generic RAG platform.

Suggested rollout:

1. transcript persistence
2. transcript chunk persistence
3. full-text transcript search
4. `pgvector`-backed embeddings and semantic retrieval
5. grounded Q&A over retrieved chunks

This gives:

- keyword search for exact terms
- semantic search for concept-level retrieval
- Q&A that cites the relevant transcript time ranges

## 10. API Surface

Add endpoints for:

- list transcriptions for a video
- get/download a specific transcription file
- optionally request transcription for a video manually
- optionally re-run transcription for admins/editors
- search within a video transcript
- ask questions about a video using retrieved transcript passages

Suggested examples:

- `GET /api/v1/videos/{videoId}/transcriptions`
- `GET /api/v1/videos/{videoId}/transcriptions/{transcriptionId}`
- `POST /api/v1/videos/{videoId}/transcriptions`
- `POST /api/v1/videos/{videoId}/transcriptions/{transcriptionId}/retry`
- `GET /api/v1/videos/{videoId}/transcript-search?q=...`
- `POST /api/v1/videos/{videoId}/questions`

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
- embedding generation cost and storage size
- search-index refresh behavior after transcript updates

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
- transcript search ranking/filtering
- Q&A retrieval grounding and cited time ranges

Use a fake transcription provider for most `.NET` tests.
Python worker smoke tests can be separate and lighter-weight.

## Acceptance Criteria

- A ready video can automatically queue a transcription job when enabled.
- `.NET` can hand work to a local Python `faster-whisper` worker through an internal provider adapter.
- Generated caption files are stored and linked through `VideoTranscriptions`.
- Transcription status is queryable through the API.
- Caption retrieval respects existing video authorization rules.
- Transcript search can return timestamped matches for a video.
- Video Q&A uses retrieved transcript passages rather than the full raw transcript.
- The feature is configurable and off by default.
- Tests cover orchestration, authorization, and failure handling.

## Notes

- Keep project-level implementation tracking in `EXECUTION_PLAN.md`.
- Keep Python-worker-specific implementation details in `services/transcription-worker/EXECUTION_PLAN.md`.
- Start with one local provider/runtime and keep the abstraction clean enough to support future hosted alternatives.
- `VTT` should be the primary player-facing output format, even if `SRT` and plain text are also stored.
