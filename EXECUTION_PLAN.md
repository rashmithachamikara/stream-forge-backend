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

### Phase 8: Engagement Features (Week 8-10)
- [ ] Implement likes/dislikes
- [ ] Add comments system
- [ ] Create bookmarks feature
- [ ] Build playlists functionality
- [ ] Add notifications

### Phase 9: Analytics (Week 10-11)
- [ ] Track video views
- [ ] Track watch time
- [ ] Create analytics dashboard
- [ ] Generate reports

### Phase 10: Testing (Week 11-12)
- [ ] Add unit tests for domain
- [ ] Add upload use-case tests deferred from Phase 5
- [ ] Add integration tests for repositories
- [ ] Add API tests for endpoints
- [ ] Add local/backend upload API tests
- [ ] Add end-to-end tests

### Phase 11: Deployment (Week 12+)
- [ ] Dockerize application
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

## Notes

- Detailed implementation notes for each phase will be maintained under `documentation/plans/`.
- This document tracks phase-level status only.
- Schema source of truth remains `documentation/schema/schema-v2.0.md`.
