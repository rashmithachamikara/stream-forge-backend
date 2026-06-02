# Phase 5 - Video Upload

Status: [X] Done

## Purpose

Deliver the core local/backend upload flow and refactor it into the Clean Architecture boundaries used by the rest of Stream Forge. This phase owns the upload session model, local chunk handling, upload API surface, and application-layer orchestration for creating an uploading video and attaching file records after chunks are completed.

Advanced upload hardening is intentionally deferred to [Phase 12 - Upload Hardening and Advanced Storage](phase-12-upload-hardening-and-advanced-storage.md).
Completed uploads hand off to [Phase 6 - Video Processing And Streaming](phase-06-video-processing.md) for HLS generation, thumbnails, and final playable `Ready` status.

## Implemented Scope

- Session-based video upload flow for authenticated users.
- Chunked local/backend upload endpoints for session creation, target resolution, part upload, and completion.
- Upload session and upload session part persistence in PostgreSQL.
- Video records are created when upload sessions start, with `Status = Uploading`.
- Upload configuration for storage path, chunk size, maximum file size, allowed MIME types, session expiration, and storage provider type.
- Upload-specific rate limit settings for target/chunk traffic.
- JS SDK helper in `sdk/js-sdk/` for chunked upload flow.
- Local/backend upload smoke flow has been run successfully.

## Current Refactor Scope

- Move upload orchestration out of `UploadSessionsController` and into Application use cases.
- Keep the controller thin: HTTP binding, authorization context, use-case calls, and DTO responses only.
- Complete and register the existing repository/unit-of-work seam for upload workflows.
- Keep local file save, chunk assembly, and chunk deletion behind `IStorageService`.
- Accept core upload metadata: title, description, total size, content type, category, visibility, and tag IDs.
- Store video metadata directly on `Video` and tags directly in `VideoTags` at session creation.
- Keep `UploadSession` limited to upload/runtime state linked to `VideoId`.
- Validate category and tag IDs before accepting an upload session.
- Validate chunk number, chunk size, checksum, contiguous parts, and final assembled size.
- Delete temporary chunk files after successful final file creation and database completion.

## Out Of Scope For Phase 5

- Real S3 multipart upload implementation and presigned URL generation.
- Resumable overwrite/retry semantics for duplicate or partially uploaded chunks.
- Background cleanup jobs for expired/failed sessions and abandoned files.
- Upload-time thumbnail, player, and access-control metadata.
- Upload observability dashboards, metrics, and alerts.

These items belong to Phase 12.

## Deliverables

- `UploadSession` and `UploadSessionPart` domain entities, EF configuration, migration, and repositories.
- `IStorageService` abstraction with local filesystem implementation for backend chunk storage and assembly.
- Application use cases for CreateSession, GetUploadTarget, UploadPart, and CompleteSession.
- Upload endpoint DTOs for session creation, target resolution, part upload, and completion.
- `UploadSessionsController` as a thin HTTP adapter over the upload use cases.
- JS SDK upload helper for local/backend chunk upload.
- Upload configuration documented in `documentation/appsettings-readme.md`.

## Dependencies

- Phase 2 domain entities: `Video`, `VideoFile`, `StorageProvider`, `User`, `Category`, `Tag`, and `VideoTag`.
- Phase 3 infrastructure: EF Core, migrations, database context, repositories, and unit of work.
- Phase 4 authentication and authorization, including current-user resolution and ownership checks.
- API configuration for chunk size, upload limits, storage root path, upload storage provider selection, and upload-specific rate limits.

## Acceptance Criteria

- An authenticated user can create an upload session and receive a session ID plus video ID.
- The client can fetch a backend upload target and upload chunks to the API.
- Upload session creation validates file size, MIME type, category, tags, and visibility.
- Upload session creation creates a `Video` with `Status = Uploading` and creates `VideoTag` rows.
- Chunk uploads validate part number, part size, configured chunk limit, and checksum.
- Completion rejects missing, non-contiguous, expired, or size-mismatched uploads.
- On completion, the API assembles chunks, creates the original `VideoVersion` and `VideoFile` records, marks the session completed, and hands the video to Phase 6 processing.
- Temporary chunk files are deleted after successful completion; cleanup failures are logged without failing a completed upload.
- The database stores upload session, session parts, video metadata, and file metadata consistently.
- The local/backend upload flow can upload and complete a sample video.
- The project builds successfully after the upload refactor.

## Notes

- Start with local/backend upload as the stable development path.
- Keep S3 placeholders from being treated as production-ready behavior until Phase 12.
- Keep transcoding, thumbnail generation, and streaming pipeline behavior outside the upload request path.
- Phase 6 owns the processing queue and marks videos `Ready` after playable assets are generated.
- Automated upload use-case, API, and large-file coverage is deferred to Phase 10 testing work.
