# Stream Forge - Execution Plan

## Phase Status

### Phase 1: Project Bootstrap and Foundation [X]
- [X] Create solution and Clean Architecture project structure
- [X] Configure project references and dependency flow
- [X] Create baseline folders for Domain, Application, Infrastructure, and API
- [X] Add baseline project documentation and development guides
- [X] Verify successful solution build

### Phase 2: Core Domain Model [X]
- [X] Implement core domain entities (19 tables aligned to schema)
- [X] Implement domain enumerations
- [X] Add domain exceptions and core interfaces
- [X] Implement access control and sharing domain concepts

### Phase 3: Infrastructure Data Layer [X]
- [X] Install EF Core and PostgreSQL packages
- [X] Implement `StreamForgeDbContext` with all required DbSets
- [X] Implement all entity configurations (Fluent API)
- [X] Align configurations with schema/domain naming
- [X] Verify successful build after infrastructure alignment

### Phase 4: Authentication and Authorization (Week 3-4) [X]
- [X] Install JWT packages
- [X] Implement JWT token service
- [X] Create authentication use cases (Login, Register, RefreshToken)
- [X] Add authentication middleware
- [X] Implement role-based authorization
- [X] Add auth abstractions (`ITokenService`, `ICurrentUserService`, `IAuthorizationService`)
- [X] Centralize video authorization rules (owner, visibility, access grants)
- [X] Add AccessControl query helpers for per-user and token-based sharing
- [X] Keep auth provider adapters pluggable for future OAuth/OIDC integration

### Phase 5: Video Upload (Week 4-5) [X]
- [X] Create storage abstraction (`IStorageService`)
- [X] Implement local storage service
- [X] Move upload orchestration into Application use cases
- [X] Implement upload repositories and unit-of-work path
- [X] Keep upload controller as a thin HTTP adapter
- [X] Add session, target, part upload, and completion endpoints
- [X] Create video at upload-session start and hand completed uploads to processing
- [X] Validate chunks, assemble final file, compute checksum, and delete temporary chunks

### Phase 6: Video Processing And Streaming (Week 5-8)
- [X] Install and configure Hangfire with PostgreSQL storage
- [X] Add processing queue integration after upload completion
- [X] Implement Application abstractions for background jobs, media processing, and streaming asset access
- [X] Implement FFmpeg/ffprobe Infrastructure adapters
- [X] Probe uploaded media and update duration, codec, bitrate, and dimensions
- [X] Generate HLS master playlist, variant playlists, and segments
- [X] Generate default thumbnail/poster assets
- [X] Persist processing job state, progress, completion, and failure details
- [X] Create/update `VideoVersions`, `VideoFiles`, and `VideoThumbnails` for generated outputs
- [X] Add manifest, segment, and thumbnail endpoints with existing video access control
- [X] Keep API controllers thin and keep Hangfire, FFmpeg, filesystem, and EF orchestration outside controllers
- [X] Run FFmpeg/Hangfire smoke test with a sample video

### Phase 7: Video Streaming (Week 7-8) [X] Folded into Phase 6
- [X] HLS playlist generation moved to Phase 6
- [X] Streaming endpoints moved to Phase 6
- [X] Range/manifest serving moved to Phase 6
- [X] Streaming access control moved to Phase 6

### Phase 7.5: Content Discovery and Management [X]
- [X] Add visibility-aware paginated video list and detail APIs
- [X] Add authenticated user video library APIs
- [X] Add video metadata update and delete/archive APIs
- [X] Add video processing-status endpoint for owner/admin progress views
- [X] Add category list/detail and category-video browse APIs
- [X] Add paginated tag list/search and tag-video browse APIs
- [X] Add basic video search, filtering, paging, and deterministic ordering
- [X] Add public/user profile metadata APIs if creator pages require them
- [X] Add video access/share-token management APIs if sharing management is required
- [X] Add shared pagination request/response DTOs for unbounded collection endpoints
- [X] Keep content controllers thin and enforce existing video authorization rules

### Phase 8: Engagement Features (Week 8-10) [X]
- [X] Implement likes/dislikes
- [X] Add comments system
- [X] Create bookmarks feature
- [X] Build playlists functionality
- [X] Add notifications

### Phase 9: Analytics (Week 10-11) [X]
- [X] Add configurable playback analytics ingestion
- [X] Track thresholded video views and watch time
- [X] Add owner analytics dashboards and rankings
- [X] Add admin analytics dashboards and platform trends
- [X] Add engagement analytics such as most liked/commented/engaged videos
- [X] Add active-viewer, peak-watch-time, and device-breakdown metrics
- [X] Add downloadable analytics reports

### Phase 10: Testing (Week 11-12)
- [X] Add test projects and baseline test infrastructure
- [X] Add starter unit tests for domain
- [X] Add starter Application tests for analytics config gating
- [X] Add starter Infrastructure/API test scaffolds
- [X] Add expanded domain tests for access controls, comments, notifications, playlists, processing jobs, bookmarks, tags, and reactions
- [X] Add category/tag admin guard tests
- [X] Add upload validation use-case tests deferred from Phase 5
- [X] Add upload success-path application tests
- [X] Add processing, video update/archive, engagement, and analytics application tests
- [X] Add repository and PostgreSQL integration tests
- [X] Add first API integration tests for auth, category/tag, video, engagement, and analytics endpoints
- [ ] Add remaining API tests for upload, playback, playlists, notifications, and analytics reporting endpoints
- [ ] Add local/backend upload API tests
- [ ] Add end-to-end tests

### Phase 11: Deployment (Week 12+)
- [X] Dockerize application
- [ ] Set up CI/CD pipeline
- [ ] Configure production database
- [ ] Deploy to cloud

### Phase 12: Upload Hardening and Advanced Storage
- [ ] Implement real S3 multipart uploads and presigned URL generation
- [ ] Add resumable retry semantics for duplicate or partially uploaded chunks
- [ ] Add cleanup jobs for expired/failed upload sessions and abandoned chunk files
- [ ] Add optional upload-time thumbnail/player/access metadata if product flow requires it
- [ ] Add upload observability for sessions, chunk failures, cleanup results, and storage usage
- [ ] Add integration tests for S3, cleanup, retries, and large-file upload flows

### Phase 13: External Identity Provider Integration
- [ ] Use external login with internal StreamForge JWTs as the first OAuth/OIDC strategy
- [ ] Add external provider configuration and enabled-provider discovery endpoint
- [ ] Add external sign-in endpoint for providers such as Google
- [ ] Add `UserExternalLogins` model and persistence for provider/subject links
- [ ] Resolve external identities to internal `Users.Id` before authorization
- [ ] Keep content, upload, playback, engagement, and analytics use cases decoupled from provider-specific claims
- [ ] Preserve a migration path to direct external bearer-token validation if enterprise OIDC requires it later
- [ ] Follow detailed implementation notes in `documentation/plans/phase-13-external-identity-provider-integration.md`

### Phase 14: Real-Time Processing Progress
- [ ] Capture FFmpeg progress using `-progress pipe:1` during HLS generation
- [ ] Estimate HLS generation percentage from processed timestamp versus probed media duration
- [ ] Add a live processing-progress store using memory cache or Redis to avoid frequent database writes
- [ ] Merge persisted job state with live progress in the processing-status endpoint
- [ ] Throttle any optional database progress writes by percentage or time interval
- [ ] Keep milestone-based DB progress as the durable fallback after restarts
- [ ] Add tests for progress parsing, throttling, and fallback behavior

### Phase 15: AI Transcription and Captions
- [X] Add transcription configuration and provider abstraction for local and hosted transcription providers
- [X] Queue transcription after upload/processing completes and source media is ready
- [X] Implement .NET transcription adapter that submits jobs to a local Python Faster-Whisper worker
- [X] Add Python transcription worker project under `services/transcription-worker/`
- [X] Define the initial internal worker contract for job submission, status polling, callback payloads, and staged artifact references
- [X] Generate staged transcript/caption outputs such as `VTT`, `SRT`, and `segments.json` in the Python worker
- [X] Refine worker progress so active transcription percentage is derived from transcribed time versus media duration
- [X] Persist canonical transcript/caption outputs through `.NET` storage and `VideoTranscriptions`
- [X] Track transcription job state, language, provider, and failure details in `VideoTranscriptions`
- [X] Add APIs for listing, retrieving, and downloading video transcriptions/captions
- [X] Add player-facing caption endpoint support using existing video authorization rules
- [X] Chunk transcripts with timestamps and persist them for transcript search and grounded Q&A foundations
- [X] Add optional auto-transcription toggle and admin-managed provider/language/model settings
- [X] Add tests for transcription orchestration, storage, authorization, and failure handling

### Phase 16: Signed Media Access URLs
- [ ] Add backend-issued signed media access for thumbnails, manifests, HLS assets, and caption files
- [ ] Add a dedicated `GET /api/v1/videos/{videoId}/media-access` endpoint for short-lived signed playback URLs
- [ ] Add stateless HMAC-based signed URL issuance and validation for local/API-served media
- [ ] Keep existing playback, thumbnail, and caption routes as delivery endpoints while adding signed-query support
- [ ] Rewrite master and variant HLS manifests to emit signed asset references
- [ ] Extend storage/media-access abstractions so future S3/object-storage providers can return presigned read URLs
- [ ] Keep current content/search DTO media URLs stable during transition, then migrate frontend clients to the media-access bundle
- [ ] Add tests for signed URL issuance, tamper/expiry rejection, manifest rewriting, and caption delivery
- [ ] Document signed media delivery flow, expiry behavior, and local-vs-S3 strategy in a dedicated phase plan

### Phase 17: Transcript Intelligence and RAG
- [X] Upgrade transcript search from keyword search to PostgreSQL full-text + pg_trgm search
- [ ] [OPTIONAL] Add transcript ranking/highlighting behavior for full-text matches over persisted chunks
- [X] Define the embedding pipeline and move embedding generation into a dedicated post-transcription stage or worker
- [ ] Add schema support for embedding storage and vector indexes in a new schema version
- [ ] Add `pgvector`-backed semantic retrieval over transcript chunks
- [ ] Add grounded video Q&A endpoints over retrieved transcript passages with cited time ranges
- [ ] Add retrieval/answering tests for full-text search, semantic search, citation integrity, and failure handling
- [ ] Add docker support

## Backlog
- [ ] Add multi-language support for PostgreSQL full-text search so transcript chunks use language-aware text search configuration instead of the current fixed English configuration

## Notes

- Detailed implementation notes for each phase will be maintained under `documentation/plans/`.
- This document tracks phase-level status only.
- Schema source of truth remains `documentation/schema/schema-v6.0.md`.
