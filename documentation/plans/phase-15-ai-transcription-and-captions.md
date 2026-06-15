# Phase 15 - AI Transcription and Captions

Status: [ ] Planned

## Purpose

Automatically transcribe uploaded videos and turn those transcripts into usable caption assets and transcript outputs.

The initial target is Whisper-based transcription triggered after a video upload has completed and the source media is ready. This phase should keep the AI provider behind an application abstraction so the system is not tightly coupled to one Whisper runtime forever.

This phase should also lay the foundation for transcript-based search and user Q&A over videos.

## Why This Fits The Current Architecture

The repo already has:

- `VideoTranscription` domain entity
- `TranscriptionStatus` enum
- `ProcessingJobType.Transcription`
- local process execution patterns already used for FFmpeg/ffprobe
- Hangfire-based background processing

That means the transcription feature should be implemented as another background-processing capability, not as synchronous request-time work.

## Recommended Approach

## 1. Trigger Point

Do not transcribe during the upload HTTP request itself.

Preferred flow:

1. upload completes
2. video processing finishes and the source/original file is known
3. transcription job is queued
4. Whisper produces transcript/caption outputs
5. `VideoTranscriptions` records are updated

This keeps large model inference out of the request path and avoids mixing upload reliability with AI runtime cost.

## 2. Provider Shape

Add an Application abstraction such as:

- `ITranscriptionService`

Suggested responsibilities:

- accept a source media path
- accept requested output formats
- accept optional language hint
- accept model/runtime options
- return generated files plus metadata

That keeps controllers and use cases decoupled from the underlying Whisper runtime.

## 3. Whisper Runtime Choice

Whisper can be integrated a few ways:

- `whisper` Python package
- `faster-whisper`
- `whisper.cpp`
- external transcription microservice that uses Whisper internally

### Recommended First Implementation

Use a local Infrastructure adapter that shells out to a Whisper-compatible runtime, following the same style already used for FFmpeg.

Best practical options:

- `faster-whisper` if GPU/CPU performance matters and Python is acceptable
- `whisper.cpp` if you want a simpler local binary dependency without Python

For this backend, `whisper.cpp` or `faster-whisper` both fit well. If the goal is the simplest operational shape alongside FFmpeg-style binaries, `whisper.cpp` is especially attractive. If the goal is better speed/quality tradeoffs and easier model choice, `faster-whisper` is a strong default.

## 4. Configuration

Add a new configuration section such as `Transcription`.

Suggested options:

- `Enabled`
- `AutoTranscribeOnReady`
- `Provider`
- `Model`
- `Language`
- `OutputFormats`
- `Device` (`cpu`, `cuda`, etc.)
- `BeamSize`
- `Prompt`
- `WordTimestampsEnabled`
- `MaxConcurrentJobs`
- `StorePlainTextTranscript`

Suggested conservative defaults:

- `Enabled = false`
- `AutoTranscribeOnReady = false`
- `Provider = whispercpp` or `faster-whisper`
- `Model = base`
- `Language = auto`
- `OutputFormats = [ "vtt", "srt" ]`

Keep this off by default because model downloads, CPU load, and storage growth are deployment concerns.

## 5. Storage Model

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
- `Source`
- timestamps

If supporting multiple formats, either:

- keep one row per `language + format`, or
- evolve the model later to group outputs under one transcription job and child files

For the first iteration, one row per output format is fine and fits the current schema well.

## 6. Transcript Search And Q&A Model

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

### Recommended Retrieval Strategy

Start with a focused transcript-based retrieval approach rather than a large, generic RAG platform.

Suggested rollout:

1. transcript storage + full-text search
2. transcript chunking + embeddings
3. grounded Q&A over retrieved chunks

This gives:

- keyword search for exact terms
- semantic search for concept-level retrieval
- Q&A that cites the relevant transcript time ranges

### Why This Is Better Than Simpler Alternatives

- full transcript prompt stuffing does not scale for long videos
- summary-only approaches do not support grounded detail retrieval
- fine-tuning is the wrong tool for per-video factual answering
- transcript retrieval keeps answers explainable and timestamp-linked

### Suggested Data Shape

Add a transcript-chunk persistence model in a later implementation step, for example:

- `VideoTranscriptChunks`

Suggested fields:

- `Id`
- `VideoId`
- `TranscriptionId`
- `Language`
- `StartSeconds`
- `EndSeconds`
- `Content`
- optional `Embedding`
- `CreatedAt`

If PostgreSQL vector search is desired, this can later integrate with `pgvector`. If not, the first iteration can still provide value with full-text indexing alone.

## 7. Background Job Flow

Add a transcription use-case/service that:

- validates transcription is enabled
- resolves the source media path
- creates or reuses pending `VideoTranscription` records
- marks them `Processing`
- invokes the Whisper adapter
- writes generated files through storage/local filesystem
- marks records `Completed` or `Failed`

Recommended Hangfire behavior:

- queue transcription only after core video processing succeeds
- retry transient runtime failures
- persist failure details in logs and, if needed later, in job error metadata

Transcription completion can also trigger:

- transcript chunk generation
- optional embedding generation
- search index updates

## 8. API Surface

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

Playback-related caption access should reuse existing video authorization rules so private videos do not expose captions publicly.

For Q&A responses, return:

- answer text
- supporting transcript snippets
- cited time ranges
- optional confidence or retrieval metadata

## 9. Player Support

Player-facing support should include:

- caption file retrieval with existing playback authorization
- lightweight metadata telling the frontend which languages/formats are available
- preference for `VTT` for HTML5 players

The React app can then attach caption tracks directly to the player.

Transcript search support can also let the player:

- jump to matched timestamps
- show transcript snippets near matches
- support “search in video” interactions

## 10. Failure And Operational Considerations

Whisper-based transcription can be expensive in CPU, RAM, disk, and time.

Plan for:

- model installation/download strategy
- CPU-only vs GPU deployments
- queue concurrency limits
- timeout handling
- large-file runtime limits
- language auto-detection inaccuracies
- reprocessing strategy when a model changes
- embedding generation cost and storage size
- search-index refresh behavior after transcript updates

Do not block upload success on transcription success.
Transcription should be an asynchronous enhancement, not part of the critical upload contract.

## 11. Testing

Add tests for:

- transcription auto-queue orchestration after video readiness
- disabled-feature behavior
- successful completion and file persistence
- failure transitions to `Failed`
- authorization for transcript/caption retrieval
- storage path generation
- per-format record handling
- transcript chunk generation
- transcript search ranking/filtering
- Q&A retrieval grounding and cited time ranges

Use a fake transcription adapter for most tests.
Any real Whisper smoke test should remain optional/manual because runtime dependencies are heavy.

## Acceptance Criteria

- A ready video can automatically queue a Whisper-based transcription job when enabled.
- Generated caption files are stored and linked through `VideoTranscriptions`.
- Transcription status is queryable through the API.
- Caption retrieval respects existing video authorization rules.
- Transcript search can return timestamped matches for a video.
- Video Q&A uses retrieved transcript passages rather than the full raw transcript.
- The feature is configurable and off by default.
- Tests cover orchestration, authorization, and failure handling.

## Notes

- This phase should follow Phase 6 processing patterns closely instead of inventing a separate AI pipeline.
- Start with one provider/runtime and keep the abstraction clean enough to support future alternatives.
- `VTT` should be the primary player-facing output format, even if `SRT` and plain text are also stored.
