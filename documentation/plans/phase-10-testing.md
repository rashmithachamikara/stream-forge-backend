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
- [X] `tests/StreamForge.Infrastructure.IntegrationTests` for opt-in PostgreSQL/Testcontainers coverage
- [X] `tests/StreamForge.Api.IntegrationTests` for opt-in API/Testcontainers coverage
- [X] Add test projects to `StreamForge.sln`
- [X] Keep PostgreSQL integration tests outside the main solution test run so editor test panels stay focused on fast tests

Repeatable command:

```bash
dotnet test StreamForge.sln --no-restore
```

PostgreSQL-backed integration tests are opt-in because they require Docker/Testcontainers:

```powershell
$env:STREAMFORGE_RUN_POSTGRES_TESTS='true'
dotnet test tests/StreamForge.Infrastructure.IntegrationTests/StreamForge.Infrastructure.IntegrationTests.csproj --filter "Category=PostgresIntegration"
```

API integration tests are also opt-in:

```powershell
$env:STREAMFORGE_RUN_API_INTEGRATION_TESTS='true'
dotnet test tests/StreamForge.Api.IntegrationTests/StreamForge.Api.IntegrationTests.csproj --filter "Category=ApiIntegration"
```

Use `dotnet restore StreamForge.sln` first after cloning or after test package changes.

## Current Coverage

- [X] Domain tests for timestamp bookmarks, playlist video counts, playlist items, tags, video reactions, comments/replies, notifications, access controls, and video processing jobs
- [X] Application tests for analytics ingestion config gating
- [X] Application tests for upload validation failure paths: authentication, file size, MIME type, missing category, invalid part number, missing checksum, and blank completion file name
- [X] Application tests for upload happy paths: session creation, upload target generation, part upload/checksum success, completion, source record creation, and processing queue enqueue
- [X] Application tests for category/tag admin create and delete guard behavior
- [X] Application tests for video metadata, engagement settings, player settings, tag replacement, archive, engagement flows, processing success/failure, and analytics reporting/query guards
- [X] Infrastructure EF model tests for bookmark/reaction/tag/playlist index behavior
- [X] PostgreSQL-backed repository tests for video visibility/search, bookmark pagination, category/tag guard queries, playlist ordering/uniqueness, playlist `VideoCount`, and analytics aggregations
- [X] Docker-backed PostgreSQL smoke-test scaffold for future integration tests, isolated in the integration test project
- [X] API `WebApplicationFactory` smoke-test scaffold for future host tests
- [X] API integration tests for auth lifecycle, protected auth endpoints, category/tag admin CRUD, video list/detail/processing-status, engagement endpoints, and analytics ingestion

## Planned Domain Coverage

- [ ] User creation, update, role changes, activation/deactivation
- [ ] Video metadata, visibility, player settings, lifecycle transitions, and view count
- [ ] Upload session and upload part state transitions
- [X] Access control user grants and share-token grants
- [X] Video processing job progress, completion, and failure
- [X] Comments and reply relationships
- [X] Notifications read/unread behavior
- [X] Playlist item ordering and playlist video-count consistency

## Planned Application Coverage

- [ ] Authentication register/login/refresh flows
- [X] Upload validation failure paths
- [X] Upload session happy path, target generation, part upload, completion, and checksum success paths
- [X] Processing use cases with fake media processing
- [X] Video metadata/category/tag update flows
- [X] Category/tag admin create and delete guards
- [ ] Category patch coverage
- [X] Tag patch/update flow coverage
- [X] Engagement flows for reactions, comments, bookmarks, and notifications
- [ ] Playlist application use-case coverage
- [X] Analytics thresholded view counting, rankings, and disabled-feature behavior
- [ ] Analytics segmentation service forwarding and CSV report coverage

## Planned Infrastructure Coverage

- [ ] EF migrations apply cleanly to PostgreSQL
- [X] Repository pagination, filtering, search, and deterministic ordering
- [X] Video visibility/search queries
- [X] Category/tag in-use guard queries
- [X] EF model configuration checks for key uniqueness/index rules
- [ ] Reaction summary regression: no parallel EF operations on a single `DbContext`
- [X] Playlist `VideoCount` persistence after add/remove
- [X] Analytics aggregation queries for views, watch time, auth/category/tag/browser/device breakdowns

## Planned API Coverage

- [X] Auth happy paths, refresh, and `/auth/me`
- [X] Unauthorized/forbidden behavior for representative protected endpoints
- [ ] Upload create/target/part/complete flow
- [ ] Playback manifest, segment, and thumbnail access rules
- [X] Video list/detail/processing-status endpoints
- [X] Category/tag read and admin mutation endpoints
- [X] Reactions, comments, and bookmarks endpoints
- [ ] Playlists, notifications, and notification delete endpoints
- [X] Analytics ingestion endpoint
- [ ] Analytics owner/admin reporting endpoints

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
- Recent regressions are covered or explicitly tracked: timestamp bookmarks and category/tag delete guards are covered now; reaction summary EF concurrency, playlist count persistence, and client-event-based analytics view counting remain planned.

## Notes

- Frontend/player tests are outside Phase 10.
- EF InMemory should not be used for repository behavior that depends on relational/PostgreSQL semantics.
- Docker-backed tests should stay categorized as `PostgresIntegration` until CI has Docker/Testcontainers support configured.
