# Phase 6 - Video Processing And Streaming

Status: [ ] Planned

## Purpose

Process completed local/backend uploads into playable streaming assets while preserving Clean Architecture boundaries. Phase 6 owns background processing, media probing, HLS generation, thumbnails, and playback endpoints.

## Goals

- Enqueue processing after a successful upload completion.
- Use Hangfire with PostgreSQL storage for durable background jobs, retries, and operational visibility.
- Keep FFmpeg and ffprobe behind Infrastructure adapters.
- Generate HLS playlists/segments and a default thumbnail/poster.
- Update media metadata from the actual file, including duration, codec, bitrate, and dimensions.
- Serve manifests, segments, and thumbnails through thin API endpoints protected by existing video authorization rules.

## Clean Architecture Design

- API layer only binds HTTP requests, calls Application use cases, and returns responses.
- Application layer owns orchestration for enqueueing processing, running processing workflows, and resolving playback assets.
- Application depends on abstractions for background jobs, media processing, storage access, authorization, repositories, and unit of work.
- Application must not directly use Hangfire APIs, FFmpeg process calls, local file paths, or EF `DbContext`.
- Domain layer keeps lifecycle behavior for `Video`, `VideoProcessingJob`, `VideoVersion`, `VideoFile`, and `VideoThumbnail`.
- Infrastructure implements Hangfire dispatching, FFmpeg/ffprobe execution, local storage access, repositories, and unit of work.

## Processing Flow

- Upload completion enqueues a video processing workflow after the upload transaction succeeds.
- The workflow creates or updates a `VideoProcessingJob` and marks the video `Processing`.
- ffprobe reads source metadata and updates `VideoVersion.DurationSeconds`, codec, bitrate, and related metadata.
- FFmpeg generates adaptive HLS outputs: 1080p, 720p, and 480p when the source supports them; do not upscale.
- FFmpeg generates a default thumbnail/poster image.
- Generated outputs create or update `VideoVersions`, `VideoFiles`, and `VideoThumbnails`.
- On success, mark the processing job completed and mark the video `Ready`.
- On terminal failure, mark the processing job failed and mark the video `Failed`.

## Streaming Flow

- Add playback use cases for resolving HLS manifests, HLS segments, and thumbnails/posters.
- Add API endpoints for master playlists, variant playlists, segments, and thumbnails.
- Endpoints must enforce existing owner, visibility, access grant, and share-token authorization rules.
- Controllers must not read files directly; they should call Application use cases that return a stream descriptor or file response model.
- Only `Ready` videos should be served as playable by default.

## Dependencies

- Phase 5 upload completion creates the source `Video`, `VideoVersion`, and `VideoFile`.
- Phase 4 authorization service provides video access checks.
- Existing schema includes `VideoProcessingJobs`, `VideoVersions`, `VideoFiles`, and `VideoThumbnails`.
- Local storage remains the first supported storage provider for generated assets.
- FFmpeg and ffprobe must be installed or configured in the runtime environment.

## Acceptance Criteria

- Upload completion enqueues a Hangfire processing job without doing processing inside the request path.
- Processing job status, progress, start time, completion time, and failure reason are persisted.
- ffprobe updates real media duration instead of leaving `DurationSeconds = 0`.
- HLS master playlist, variant playlists, and segments are generated for supported source videos.
- A default thumbnail is generated and stored.
- Generated outputs are represented in `VideoVersions`, `VideoFiles`, and `VideoThumbnails`.
- Video status transitions through `Processing` and becomes `Ready` only after playable assets exist.
- Terminal processing failures mark both the job and video failed.
- Manifest, segment, and thumbnail endpoints enforce video access control.
- API controllers remain thin HTTP adapters; Infrastructure owns Hangfire, FFmpeg, and filesystem details.
- The project builds successfully after implementation.

## Notes

- Phase 7 streaming work has been folded into this phase.
- Do not add S3 multipart upload, resumable upload retry semantics, upload cleanup jobs, or advanced upload observability here; those remain Phase 12.
- Start with local storage and HLS playback. Additional storage providers can extend the same Application abstractions later.
- Automated test coverage for processing and streaming can be tracked under Phase 10 unless implemented directly during this phase.
