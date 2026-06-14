# Phase 10 - Testing

Status: [ ] In Progress

## Purpose

Add automated confidence around the backend features completed through Phase 9: authentication, uploads, processing/playback, content management, engagement, and analytics.

Phase 10 starts by adding the first real test infrastructure to the solution, then expands coverage by layer.

## Test Stack

- `xUnit` for tests
- `FluentAssertions` for assertions
- `NSubstitute` for mocks/fakes
- `Microsoft.AspNetCore.Mvc.Testing` for API host tests
- `Testcontainers.PostgreSql` for PostgreSQL-backed integration/API tests

## Test Projects

- [X] `tests/StreamForge.Domain.Tests`
- [X] `tests/StreamForge.Application.Tests`
- [X] `tests/StreamForge.Infrastructure.Tests`
- [X] `tests/StreamForge.Api.Tests`
- [X] Add test projects to `StreamForge.sln`

Repeatable command:

```bash
dotnet test StreamForge.sln --no-restore
```

Use `dotnet restore StreamForge.sln` first after cloning or after test package changes.

## Current Starter Coverage

- [X] Domain tests for timestamp bookmarks, playlist video counts, tags, and video reactions
- [X] Application tests for analytics ingestion config gating
- [X] Infrastructure EF model tests for bookmark/reaction index behavior
- [X] Docker-backed PostgreSQL smoke-test scaffold for future integration tests
- [X] API `WebApplicationFactory` smoke-test scaffold for future host tests

## Planned Domain Coverage

- [ ] User creation, update, role changes, activation/deactivation
- [ ] Video metadata, visibility, player settings, lifecycle transitions, and view count
- [ ] Upload session and upload part state transitions
- [ ] Access control user grants and share-token grants
- [ ] Video processing job progress, completion, and failure
- [ ] Comments and reply relationships
- [ ] Notifications read/unread behavior
- [ ] Playlist item ordering and playlist video-count consistency

## Planned Application Coverage

- [ ] Authentication register/login/refresh flows
- [ ] Upload session creation, target generation, part upload, completion, checksum validation, and failure paths
- [ ] Processing use cases with fake media processing and fake storage
- [ ] Video metadata/category/tag update flows
- [ ] Category/tag admin create, patch, delete guards
- [ ] Engagement flows for reactions, comments, bookmarks, playlists, and notifications
- [ ] Analytics thresholded view counting, rankings, segmentation, CSV reports, and disabled-feature behavior

## Planned Infrastructure Coverage

- [ ] EF migrations apply cleanly to PostgreSQL
- [ ] Repository pagination, filtering, search, and deterministic ordering
- [ ] Video visibility/search queries
- [ ] Category/tag in-use guard queries
- [ ] Reaction summary regression: no parallel EF operations on a single `DbContext`
- [ ] Playlist `VideoCount` persistence after add/remove
- [ ] Analytics aggregation queries for views, watch time, auth/category/tag/browser/device breakdowns

## Planned API Coverage

- [ ] Auth happy paths, invalid credentials, refresh, and `/auth/me`
- [ ] Unauthorized/forbidden behavior for protected endpoints
- [ ] Upload create/target/part/complete flow
- [ ] Playback manifest, segment, and thumbnail access rules
- [ ] Video list/detail/update/archive/processing-status endpoints
- [ ] Category/tag read and admin mutation endpoints
- [ ] Reactions, comments, bookmarks, playlists, notifications, and notification delete endpoints
- [ ] Analytics ingestion plus owner/admin reporting endpoints

## Local End-To-End Smoke Path

- [ ] Create user/editor/admin fixtures
- [ ] Create upload session
- [ ] Upload a small sample file in chunks
- [ ] Complete upload
- [ ] Assert the video enters the processing path
- [ ] Use fake FFmpeg/media processing by default so CI stays reliable
- [ ] Keep real FFmpeg smoke testing optional/manual unless CI explicitly provides FFmpeg

## Acceptance Criteria

- `dotnet test StreamForge.sln --no-restore` runs the default non-Docker test suite.
- Unit tests do not require PostgreSQL, filesystem storage, Hangfire, or FFmpeg.
- Integration/API tests use isolated PostgreSQL databases through Testcontainers when enabled.
- Temp storage paths are used for filesystem-dependent tests and cleaned after each run.
- Recent regressions are covered: reaction summary EF concurrency, playlist count persistence, timestamp bookmarks, category/tag delete guards, and client-event-based analytics view counting.

## Notes

- Frontend/player tests are outside Phase 10.
- EF InMemory should not be used for repository behavior that depends on relational/PostgreSQL semantics.
- Docker-backed tests may stay skipped until CI has Docker/Testcontainers support configured.
