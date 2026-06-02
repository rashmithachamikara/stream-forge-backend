# Stream Forge App Settings README

**Date:** June 2, 2026

This document describes application-level configuration values used by Stream Forge. These settings are not database schema, but they affect runtime behavior such as upload limits, chunk handling, rate limiting, storage location, authentication, and CORS.

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

**Notes:**
- Upload target and chunk endpoints use a separate rate-limit bucket so large videos do not exhaust the general API quota.
- Tune `UploadPermitLimit` based on configured chunk size, client concurrency, expected file sizes, and deployment infrastructure.

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

---

## Cors

The `Cors` section controls allowed browser origins.

| Setting | Description |
|---------|-------------|
| `AllowedOrigins` | List of frontend origins allowed to call the API |

**Notes:**
- Development can allow local frontend origins.
- Production should list only trusted domains.

