# Stream Forge App Settings README

**Date:** June 7, 2026

This document describes application-level configuration values used by Stream Forge. These settings are not database schema, but they affect runtime behavior such as upload limits, chunk handling, rate limiting, storage location, authentication, analytics, and CORS.

---

## Database

The `Database` section controls startup-time migration and seeding behavior.

| Setting | Default | Description |
|---------|---------|-------------|
| `ApplyMigrationsOnStartup` | `false` | Applies pending EF Core migrations during application startup |
| `SeedOnStartup` | `true` | Runs the app's idempotent baseline seeding when the schema is current |
| `WarnOnPendingMigrations` | `true` | Logs a warning when pending migrations exist and auto-migration is disabled |

**Notes:**
- Keep `ApplyMigrationsOnStartup=false` by default for safer production behavior.
- When `ApplyMigrationsOnStartup=true`, the app applies migrations before running startup seeding.
- When `SeedOnStartup=true` but pending migrations still exist, seeding is skipped until the schema is current.
- This section is a runtime behavior toggle set, not a replacement for a deliberate migration step in CI/CD.

---

## Upload

The `Upload` section controls video upload behavior and interacts with the `UploadSessions` and `UploadSessionParts` tables.

| Setting | Default | Description |
|---------|---------|-------------|
| `StoragePath` | `./uploads` | Root path for local upload sessions, permanent source files, and generated processing assets |
| `MaxFileSize` | `5368709120` | Maximum upload size in bytes, currently 5 GiB |
| `ChunkSize` | `5242880` | Maximum chunk size in bytes, currently 5 MiB |
| `AllowedMimeTypes` | video MIME allow-list | MIME types accepted for video uploads |
| `SessionExpirationMinutes` | `1440` | Time before incomplete upload sessions expire |
| `StorageProviderType` | `local` | Upload target provider used when creating sessions |

**Notes:**
- With 5 MiB chunks, a 1.6 GiB upload requires roughly 328 part uploads.
- If the client requests an upload target per chunk, the same upload may also make roughly 328 target requests.
- Keep `MaxFileSize`, `ChunkSize`, reverse-proxy body limits, and client upload concurrency aligned.
- Local upload session files are staged under `sessions/{sessionId}` inside `StoragePath`; completed source files are promoted to `videos/{videoId}/original`.

---

## RateLimiter

The `RateLimiter` section controls per-client request throttling.

| Setting | Default | Description |
|---------|---------|-------------|
| `PermitLimit` | `100` | General API request limit per window |
| `WindowMinutes` | `1` | General API limiter window length |
| `SegmentsPerWindow` | `8` | Sliding-window segment count for general API traffic |
| `UploadPermitLimit` | `1000` | Upload target/chunk request limit per window |
| `UploadWindowMinutes` | `1` | Upload limiter window length |
| `UploadSegmentsPerWindow` | `8` | Sliding-window segment count for upload traffic |
| `PlaybackPermitLimit` | `5000` | Playback manifest/segment/thumbnail request limit per window |
| `PlaybackWindowMinutes` | `1` | Playback limiter window length |
| `PlaybackSegmentsPerWindow` | `8` | Sliding-window segment count for playback traffic |

**Notes:**
- Upload target and chunk endpoints use a separate rate-limit bucket so large videos do not exhaust the general API quota.
- HLS playback and thumbnail endpoints use a separate rate-limit bucket because players may request many small manifests and segments during normal viewing.
- Tune `UploadPermitLimit` based on configured chunk size, client concurrency, expected file sizes, and deployment infrastructure.
- Tune `PlaybackPermitLimit` based on HLS segment duration, player behavior, expected concurrent viewers per IP, and whether a CDN or reverse proxy sits in front of the API.

---

## VideoProcessing

The `VideoProcessing` section controls local FFmpeg/ffprobe processing for generated playback assets.

| Setting | Default | Description |
|---------|---------|-------------|
| `FfmpegPath` | `ffmpeg` | Executable path or command name for FFmpeg |
| `FfprobePath` | `ffprobe` | Executable path or command name for ffprobe |
| `HlsSegmentSeconds` | `6` | Target duration for generated HLS segments |
| `ThumbnailTimestampPercent` | `10` | Percent into the video where the default thumbnail is captured |

**Notes:**
- FFmpeg and ffprobe must be installed in the runtime environment or configured with absolute paths.
- Generated HLS and thumbnail assets are stored under the configured `Upload.StoragePath`.
- Hangfire uses `ConnectionStrings.DefaultConnection` for durable processing jobs.

---

## Analytics

The `Analytics` section controls playback analytics ingestion, reporting availability, and high-write collection paths.

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Master switch for analytics ingestion and reporting |
| `IngestionEnabled` | `true` | Enables `POST /api/v1/videos/{videoId}/analytics/events` |
| `ReportingEnabled` | `true` | Enables owner and current-user analytics/reporting endpoints |
| `AdminReportingEnabled` | `true` | Enables admin analytics/reporting endpoints |
| `CollectRawEvents` | `true` | Persists playback analytics events to the analytics event store |
| `CollectAnonymousEvents` | `true` | Allows anonymous viewers to send playback analytics |
| `CollectUserAgent` | `true` | Stores request user-agent values for analytics use |
| `CollectIpAddress` | `true` | Stores request IP address values for analytics use |
| `CollectPauseEvents` | `true` | Accepts `Pause` analytics events |
| `CollectSeekEvents` | `true` | Accepts `Seek` analytics events |
| `CollectCloseEvents` | `true` | Accepts `Close` analytics events |
| `EnableDeviceBreakdown` | `true` | Enables device-type breakdown reporting |
| `EnableBrowserBreakdown` | `true` | Enables browser-family breakdown reporting |
| `EnableActiveViewerMetrics` | `true` | Enables active-viewer reporting |
| `EnablePeakWatchTimeMetrics` | `true` | Enables hour-of-day peak watch-time reporting |
| `MinimumViewWatchSeconds` | `30` | Counted-view threshold in seconds for one `VideoId + SessionId` |
| `ActiveViewerWindowMinutes` | `5` | Time window used for "active viewers" metrics |

**Notes:**
- Analytics views are derived from client-sent playback events, not from HLS manifest or segment fetches.
- When `CollectRawEvents=false`, the ingestion endpoint returns successfully but does not persist raw events.
- When `CollectAnonymousEvents=false`, anonymous playback requests are ignored for analytics even if the video is publicly viewable.
- When event toggles such as `CollectSeekEvents=false` are disabled, those event types are rejected by analytics ingestion.
- `MinimumViewWatchSeconds` controls when a session becomes a counted view; it is not the same thing as a raw playback request count.
- Device and browser breakdown endpoints depend on the related feature toggles being enabled.
- See [ANALYTICS.md](/c:/Files/Shared/Software%20Projects/Stream%20Forge/stream-forge-backend/documentation/ANALYTICS.md) for the full analytics model and frontend integration guidance.

---

## Jwt

The `Jwt` section controls access-token and refresh-token behavior.

| Setting | Description |
|---------|-------------|
| `Issuer` | Token issuer name |
| `Audience` | Token audience |
| `SigningKey` | Secret key used to sign JWTs |
| `AccessTokenMinutes` | Access token lifetime in minutes |
| `RefreshTokenDays` | Refresh token lifetime in days |

**Notes:**
- Use a strong environment-specific `SigningKey`.
- Do not commit production secrets to source control.

---

## ConnectionStrings

The `ConnectionStrings` section stores database connection strings.

| Setting | Description |
|---------|-------------|
| `DefaultConnection` | PostgreSQL connection string used by the API |

**Notes:**
- Both EF Core application data and Hangfire durable job storage use `DefaultConnection`.
- In Docker Compose, this is typically passed as `ConnectionStrings__DefaultConnection`.

---

## Cors

The `Cors` section controls allowed browser origins.

| Setting | Description |
|---------|-------------|
| `AllowedOrigins` | List of frontend origins allowed to call the API |

**Notes:**
- Development can allow local frontend origins.
- Production should list only trusted domains.

---

## Logging

The `Logging` section controls ASP.NET Core and framework log verbosity.

| Setting | Description |
|---------|-------------|
| `LogLevel.Default` | Default application log level |
| `LogLevel.Microsoft.AspNetCore` | ASP.NET Core framework log level |
| `LogLevel.Microsoft.EntityFrameworkCore.Database.Command` | EF Core SQL command log level when configured |

**Notes:**
- `appsettings.Development.json` can override logging more aggressively than the base `appsettings.json`.
- EF Core SQL logging is useful when debugging queries, but can get noisy in normal development and production.

---

## AllowedHosts

`AllowedHosts` is the standard ASP.NET Core host-filtering setting.

| Setting | Description |
|---------|-------------|
| `AllowedHosts` | Controls which host headers the app accepts |

**Notes:**
- The current base configuration uses `*`.
- Tighten this in production when you want stricter host filtering.

