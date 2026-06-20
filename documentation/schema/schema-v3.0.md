# Stream Forge Database Schema v3.0

**Version:** 3.0  
**Date:** June 20, 2026  
**Status:** Updated - Bookmark Redesign and Phase 15 Transcription Alignment

---

## Overview

This document defines the complete database schema for Stream Forge, a self-hosted video-on-demand platform. The schema is designed to support:
- Multi-user video management
- Multiple video versions (resolutions, formats)
- Pluggable storage providers
- Resumable chunked video uploads with session tracking
- Background video processing
- HLS streaming
- User engagement (likes, comments, bookmarks)
- Analytics tracking
- Access control and sharing

---

## Database Tables

### 1. Users

Stores user account information.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique user identifier |
| Name | VARCHAR(255) | NOT NULL | User's display name |
| Email | VARCHAR(255) | NOT NULL, UNIQUE | User's email address |
| PasswordHash | VARCHAR(512) | NOT NULL | Hashed password |
| Role | ENUM | NOT NULL | User role: Admin, Editor, Viewer |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | Account status |
| CreatedAt | TIMESTAMP | NOT NULL | Account creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes:**
- `IX_Users_Email` (UNIQUE)
- `IX_Users_Role`

**Notes:**
- Role is stored as enum (Admin, Editor, Viewer) - no separate Roles table needed for MVP
- Email is unique and used for authentication

---

### 2. Videos

Core video metadata and settings.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique video identifier |
| Title | VARCHAR(500) | NOT NULL | Video title |
| Description | TEXT | NULL | Video description |
| UploaderId | GUID | FK -> Users(Id), NOT NULL | User who uploaded the video |
| CategoryId | GUID | FK -> Categories(Id), NULL | Video category |
| Visibility | ENUM | NOT NULL | Public, Private, Internal |
| Status | ENUM | NOT NULL, DEFAULT Ready | Uploading, Processing, Ready, Failed, Deleted |
| AllowComments | BOOLEAN | NOT NULL, DEFAULT TRUE | Enable comments |
| AllowLikes | BOOLEAN | NOT NULL, DEFAULT TRUE | Enable likes |
| Autoplay | BOOLEAN | NOT NULL, DEFAULT FALSE | Autoplay setting |
| Loop | BOOLEAN | NOT NULL, DEFAULT FALSE | Loop playback |
| DefaultVolume | INT | NOT NULL, DEFAULT 100 | Default volume (0-100) |
| CaptionsEnabled | BOOLEAN | NOT NULL, DEFAULT TRUE | Enable captions |
| PlayerTheme | VARCHAR(50) | NOT NULL, DEFAULT 'default' | Player theme |
| ViewCount | BIGINT | NOT NULL, DEFAULT 0 | Total view count |
| CreatedAt | TIMESTAMP | NOT NULL | Upload timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes:**
- `IX_Videos_UploaderId`
- `IX_Videos_CategoryId`
- `IX_Videos_Visibility`
- `IX_Videos_Status`
- `IX_Videos_CreatedAt`

**Notes:**
- VideoSettings merged into Videos table (1:1 relationship eliminated)
- ViewCount denormalized for performance
- Upload-created videos start as `Uploading`, move to `Processing` after the final source file is assembled, and become `Ready` after processing generates playable assets.

---

### 3. VideoVersions

Different resolutions/formats of the same video.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique version identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Parent video |
| Resolution | VARCHAR(20) | NOT NULL | 1080p, 720p, 480p, 360p, 240p |
| Format | VARCHAR(20) | NOT NULL | mp4, webm, hls, dash |
| Bitrate | INT | NULL | Bitrate in kbps |
| Codec | VARCHAR(50) | NULL | Video codec (h264, h265, vp9) |
| StoragePath | VARCHAR(1000) | NOT NULL | Path to video file |
| SizeBytes | BIGINT | NOT NULL | File size in bytes |
| DurationSeconds | INT | NOT NULL | Playback duration in seconds; `0` means unknown until media probing completes |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Indexes:**
- `IX_VideoVersions_VideoId`
- `IX_VideoVersions_Resolution`

**Notes:**
- Each video can have multiple versions for adaptive streaming
- StoragePath is relative to StorageProvider configuration
- Original upload source files are stored under a canonical video-owned storage path after upload completion, not under the temporary upload-session path.

---

### 4. StorageProviders

Configuration for different storage backends.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique provider identifier |
| Name | VARCHAR(100) | NOT NULL, UNIQUE | Provider name |
| Type | ENUM | NOT NULL | Local, S3, AzureBlob |
| Configuration | JSON | NOT NULL | Provider-specific config |
| IsDefault | BOOLEAN | NOT NULL, DEFAULT FALSE | Default storage provider |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | Provider status |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes:**
- `IX_StorageProviders_Name` (UNIQUE)
- `IX_StorageProviders_IsDefault`
- `UQ_StorageProviders_Default_True` (UNIQUE WHERE IsDefault = TRUE)

**Notes:**
- Configuration JSON contains keys, bucket names, paths, etc.
- Only one provider can be IsDefault=true

---

### 5. VideoFiles

Physical file storage references.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique file identifier |
| VideoVersionId | GUID | FK -> VideoVersions(Id), NOT NULL | Associated video version |
| StorageProviderId | GUID | FK -> StorageProviders(Id), NOT NULL | Storage provider |
| FilePath | VARCHAR(1000) | NOT NULL | Full file path |
| FileSize | BIGINT | NOT NULL | File size in bytes |
| Checksum | VARCHAR(128) | NULL | File checksum (SHA-256) |
| MimeType | VARCHAR(100) | NOT NULL | MIME type |
| CreatedAt | TIMESTAMP | NOT NULL | Upload timestamp |

**Indexes:**
- `IX_VideoFiles_VideoVersionId`
- `IX_VideoFiles_StorageProviderId`

**Notes:**
- Supports multiple storage providers per video version
- Checksum for integrity verification
- The original uploaded source file path should point to permanent video storage, such as `videos/{videoId}/original/{fileName}` for local storage.

---

### 6. UploadSessions

Tracks resumable video upload runtime state for an already-created `Video`.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique upload session identifier |
| UserId | GUID | FK -> Users(Id), NOT NULL | User who owns the upload |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video created when the upload session started |
| Status | ENUM | NOT NULL | Created, Active, Completing, Completed, Failed, Expired |
| TotalSize | BIGINT | NOT NULL | Total expected upload size in bytes |
| UploadedSize | BIGINT | NOT NULL, DEFAULT 0 | Bytes received so far |
| StorageProviderType | ENUM | NOT NULL | Local, S3, AzureBlob |
| TemporaryStoragePath | VARCHAR(2000) | NOT NULL | Temporary local path, object prefix, or provider upload identifier |
| ContentType | VARCHAR(100) | NULL | Uploaded file MIME type |
| ExpiresAt | TIMESTAMP | NOT NULL | Session expiration timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last session state update |
| CreatedAt | TIMESTAMP | NOT NULL | Session creation timestamp |

**Indexes:**
- `IX_UploadSessions_UserId`
- `IX_UploadSessions_Status`
- `IX_UploadSessions_ExpiresAt`
- `IX_UploadSessions_VideoId`

**Notes:**
- Upload sessions are owned by a user and linked to the video created at upload start.
- Real video metadata is stored directly on `Videos` and related tables such as `VideoTags`.
- `Status` drives upload lifecycle handling and cleanup of incomplete uploads.
- Local upload chunks and assembled temporary files are staged under `sessions/{sessionId}` inside the configured upload storage root.
- Temporary upload paths are runtime staging locations only; completed original source files are promoted into video-owned storage before `VideoFiles` rows are committed.

---

### 7. UploadSessionParts

Stores individual chunks for a resumable upload session.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique upload part identifier |
| UploadSessionId | GUID | FK -> UploadSessions(Id), NOT NULL | Parent upload session |
| PartNumber | INT | NOT NULL | 1-based chunk number |
| Size | BIGINT | NOT NULL | Chunk size in bytes |
| Checksum | VARCHAR(256) | NOT NULL | MD5, SHA-256, or provider checksum for the chunk |
| StoragePath | VARCHAR(2000) | NOT NULL | Temporary chunk storage path or object key |
| IsComplete | BOOLEAN | NOT NULL | Whether the part upload is complete |
| UploadedAt | TIMESTAMP | NOT NULL | Timestamp when the part completed |
| CreatedAt | TIMESTAMP | NOT NULL | Part row creation timestamp |

**Indexes:**
- `IX_UploadSessionParts_UploadSessionId`
- `IX_UploadSessionParts_SessionId_PartNumber` (UNIQUE)
- `IX_UploadSessionParts_IsComplete`

**Notes:**
- Unique `(UploadSessionId, PartNumber)` prevents duplicate chunk numbers in a session.
- Parts are cascade-deleted when their parent upload session is deleted.
- `IsComplete` supports future multipart providers where a part row can exist before the provider confirms completion.

---

### 8. VideoThumbnails

Video thumbnail images.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique thumbnail identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Parent video |
| StoragePath | VARCHAR(1000) | NOT NULL | Thumbnail file path |
| Width | INT | NOT NULL | Image width in pixels |
| Height | INT | NOT NULL | Image height in pixels |
| IsDefault | BOOLEAN | NOT NULL, DEFAULT FALSE | Default thumbnail |
| TimestampSeconds | INT | NULL | Video timestamp for thumbnail |
| SizeBytes | BIGINT | NOT NULL | File size |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Indexes:**
- `IX_VideoThumbnails_VideoId`
- `IX_VideoThumbnails_IsDefault`

**Notes:**
- Multiple thumbnails per video supported
- TimestampSeconds indicates which frame was captured

---

### 9. VideoProcessingJobs

Tracks background video processing tasks.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique job identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video being processed |
| JobType | ENUM | NOT NULL | Transcode, Thumbnail, Transcription, Analysis |
| Status | ENUM | NOT NULL | Pending, Processing, Completed, Failed |
| Progress | INT | NOT NULL, DEFAULT 0 | Progress percentage (0-100) |
| ErrorMessage | TEXT | NULL | Error details if failed |
| StartedAt | TIMESTAMP | NULL | Processing start time |
| CompletedAt | TIMESTAMP | NULL | Processing completion time |
| CreatedAt | TIMESTAMP | NOT NULL | Job creation timestamp |

**Indexes:**
- `IX_VideoProcessingJobs_VideoId`
- `IX_VideoProcessingJobs_Status`
- `IX_VideoProcessingJobs_JobType`

**Notes:**
- Used by background job processors (Hangfire/Quartz)
- Progress tracked for real-time updates

---

### 10. VideoTranscriptions

AI-generated video transcriptions and subtitles.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique transcription identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Parent video |
| Language | VARCHAR(10) | NOT NULL | ISO language code (en, es, fr) |
| Format | VARCHAR(10) | NOT NULL | SRT, VTT |
| StoragePath | VARCHAR(1000) | NOT NULL | Transcription file path |
| Status | ENUM | NOT NULL | Pending, Processing, Completed, Failed |
| Source | VARCHAR(50) | NOT NULL | Provider/runtime source (local-faster-whisper, OpenAI, Azure, Manual) |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NULL | Last update timestamp |

**Indexes:**
- `IX_VideoTranscriptions_VideoId`
- `UQ_VideoTranscriptions_VideoId_Language_Format` (UNIQUE)

**Notes:**
- Supports multiple languages per video
- Supports multiple stored caption/transcription artifacts per language, such as both `VTT` and `SRT`
- The current row model is one artifact per `(VideoId, Language, Format)`
- Canonical storage paths should follow a pattern such as:
  - `videos/{videoId}/transcriptions/{language}/captions.vtt`
  - `videos/{videoId}/transcriptions/{language}/captions.srt`
- `Status` tracks the application-owned transcription lifecycle
- Near-term Phase 15 implementation may later expand this table with worker/provider metadata such as worker job ids, model, and failure details

---

### 11. Categories

Video categorization.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique category identifier |
| Name | VARCHAR(100) | NOT NULL, UNIQUE | Category name |
| Description | TEXT | NULL | Category description |
| ParentCategoryId | GUID | FK -> Categories(Id), NULL | Parent category (hierarchical) |
| DisplayOrder | INT | NOT NULL, DEFAULT 0 | Display order |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Indexes:**
- `IX_Categories_Name` (UNIQUE)
- `IX_Categories_ParentCategoryId`

**Notes:**
- Supports hierarchical categories
- DisplayOrder for custom sorting

---

### 12. Tags

Video tagging system.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique tag identifier |
| Name | VARCHAR(100) | NOT NULL, UNIQUE | Tag name |
| UsageCount | INT | NOT NULL, DEFAULT 0 | Number of videos using this tag |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Indexes:**
- `IX_Tags_Name` (UNIQUE)
- `IX_Tags_UsageCount`

**Notes:**
- UsageCount denormalized for performance

---

### 13. VideoTags (Join Table)

Many-to-many relationship between Videos and Tags.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video identifier |
| TagId | GUID | FK -> Tags(Id), NOT NULL | Tag identifier |
| CreatedAt | TIMESTAMP | NOT NULL | Association timestamp |

**Primary Key:** Composite (VideoId, TagId)

**Indexes:**
- `IX_VideoTags_VideoId`
- `IX_VideoTags_TagId`

---

### 14. AccessControl

Video access permissions and sharing tokens.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique access control identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video identifier |
| UserId | GUID | FK -> Users(Id), NULL | User (null for token-based) |
| ShareToken | VARCHAR(100) | NULL, UNIQUE | Secure sharing token |
| PermissionType | ENUM | NOT NULL | View, Embed, Download |
| ExpiresAt | TIMESTAMP | NULL | Expiration timestamp |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | Active status |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Check Constraints:**
- `CHK_AccessControl_UserOrToken` (UserId IS NOT NULL OR ShareToken IS NOT NULL)
- `CHK_AccessControl_NoDualPrincipal` (NOT (UserId IS NOT NULL AND ShareToken IS NOT NULL))

**Indexes:**
- `IX_AccessControl_VideoId`
- `IX_AccessControl_UserId`
- `IX_AccessControl_ShareToken` (UNIQUE)
- `IX_AccessControl_ExpiresAt`

**Notes:**
- Used for temporary sharing and embeds
- ShareToken enables secure public sharing without authentication
- Role-based permissions handled in application layer

---

### 15. Playlists

User-created video playlists.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique playlist identifier |
| Name | VARCHAR(255) | NOT NULL | Playlist name |
| Description | TEXT | NULL | Playlist description |
| OwnerId | GUID | FK -> Users(Id), NOT NULL | Playlist owner |
| Visibility | ENUM | NOT NULL | Public, Private |
| VideoCount | INT | NOT NULL, DEFAULT 0 | Number of videos |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes:**
- `IX_Playlists_OwnerId`
- `IX_Playlists_Visibility`

**Notes:**
- VideoCount denormalized for performance

---

### 16. PlaylistVideos (Join Table)

Many-to-many relationship between Playlists and Videos with ordering.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| PlaylistId | GUID | FK -> Playlists(Id), NOT NULL | Playlist identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video identifier |
| OrderIndex | INT | NOT NULL | Display order in playlist |
| AddedAt | TIMESTAMP | NOT NULL | Addition timestamp |

**Primary Key:** Composite (PlaylistId, VideoId)

**Indexes:**
- `IX_PlaylistVideos_PlaylistId_OrderIndex`
- `IX_PlaylistVideos_VideoId`

**Notes:**
- OrderIndex allows custom video ordering in playlists

---

### 17. Bookmarks

User-owned in-video bookmarks with timestamps and optional notes.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique bookmark identifier |
| UserId | GUID | FK -> Users(Id), NOT NULL | User identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video identifier |
| TimestampSeconds | INT | NOT NULL | Playback position in whole seconds |
| Note | TEXT | NULL | Optional user note for the bookmarked moment |
| CreatedAt | TIMESTAMP | NOT NULL | Bookmark creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last bookmark update timestamp |

**Indexes:**
- `IX_Bookmarks_UserId`
- `IX_Bookmarks_VideoId`
- `IX_Bookmarks_UserId_VideoId`
- `IX_Bookmarks_UserId_VideoId_TimestampSeconds`

**Notes:**
- Users can create multiple bookmarks per video
- Bookmarks are private to the owning user
- Resume position tracking is not stored in this table

---

### 18. VideoReactions

User likes/dislikes on videos.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique reaction identifier |
| UserId | GUID | FK -> Users(Id), NOT NULL | User identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video identifier |
| ReactionType | ENUM | NOT NULL | Like, Dislike |
| CreatedAt | TIMESTAMP | NOT NULL | Reaction timestamp |

**Indexes:**
- `IX_VideoReactions_UserId`
- `IX_VideoReactions_VideoId`
- `UQ_VideoReactions_UserId_VideoId` (UNIQUE)

**Notes:**
- Unique constraint: one reaction per user per video
- Reaction can be updated (Like ↔ Dislike)

---

### 19. VideoComments

User comments on videos with reply support.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique comment identifier |
| UserId | GUID | FK -> Users(Id), NOT NULL | Commenter |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video identifier |
| ParentCommentId | GUID | FK -> VideoComments(Id), NULL | Parent comment for replies |
| Comment | TEXT | NOT NULL | Comment text |
| IsEdited | BOOLEAN | NOT NULL, DEFAULT FALSE | Edit status |
| CreatedAt | TIMESTAMP | NOT NULL | Comment timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes:**
- `IX_VideoComments_VideoId`
- `IX_VideoComments_UserId`
- `IX_VideoComments_ParentCommentId`
- `IX_VideoComments_CreatedAt`

**Notes:**
- Supports threaded replies via ParentCommentId
- IsEdited flag for transparency

---

### 20. Notifications

User notification system.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique notification identifier |
| UserId | GUID | FK -> Users(Id), NOT NULL | Recipient user |
| VideoId | GUID | FK -> Videos(Id), NULL | Related video |
| NotificationType | ENUM | NOT NULL | Comment, Like, Upload, ProcessingComplete, Reply |
| Message | TEXT | NOT NULL | Notification message |
| IsRead | BOOLEAN | NOT NULL, DEFAULT FALSE | Read status |
| CreatedAt | TIMESTAMP | NOT NULL | Notification timestamp |

**Indexes:**
- `IX_Notifications_UserId_IsRead`
- `IX_Notifications_CreatedAt`

**Notes:**
- Supports various notification types
- VideoId nullable for non-video notifications

---

### 21. AnalyticsEvents

Video viewing and interaction analytics.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique event identifier |
| VideoId | GUID | FK -> Videos(Id), NOT NULL | Video identifier |
| UserId | GUID | FK -> Users(Id), NULL | User (null for anonymous) |
| SessionId | GUID | NOT NULL | Viewing session identifier |
| EventType | ENUM | NOT NULL | Play, Pause, Seek, Complete, Close |
| EventTime | TIMESTAMP | NOT NULL | Event timestamp |
| Position | INT | NULL | Playback position in seconds |
| DurationWatched | INT | NULL | Duration watched in this event |
| UserAgent | VARCHAR(500) | NULL | Browser user agent |
| IpAddress | VARCHAR(45) | NULL | User IP address (IPv4/IPv6) |
| CreatedAt | TIMESTAMP | NOT NULL | Record creation timestamp |

**Indexes:**
- `IX_AnalyticsEvents_VideoId_EventTime`
- `IX_AnalyticsEvents_SessionId`
- `IX_AnalyticsEvents_EventType`
- `IX_AnalyticsEvents_CreatedAt` (for partitioning)

**Notes:**
- SessionId groups events in a single viewing session
- Consider partitioning by date for performance
- May migrate to time-series DB (TimescaleDB) in future

---

## Enumerations

### UserRole
- `Admin` - Full system access
- `Editor` - Can upload and manage videos
- `Viewer` - Can watch videos and interact

### VideoVisibility
- `Public` - Accessible to all
- `Private` - Only accessible by owner
- `Internal` - Accessible to authenticated users

### VideoStatus
- `Uploading` - Upload session has started but no playable file exists yet
- `Processing` - File exists and downstream processing is running
- `Ready` - Video is ready to be listed and played
- `Failed` - Upload or processing failed
- `Deleted` - Video has been deleted or tombstoned

### StorageProviderType
- `Local` - Local filesystem
- `S3` - AWS S3 or S3-compatible
- `AzureBlob` - Azure Blob Storage

### UploadSessionStatus
- `Created` - Session created, awaiting first chunk
- `Active` - Chunks are being uploaded
- `Completing` - Final assembly is in progress
- `Completed` - Upload completed and linked video has been handed to processing
- `Failed` - Upload failed or was cancelled
- `Expired` - Session expired before completion

### VideoFormat
- `mp4` - MP4 container
- `webm` - WebM container
- `hls` - HLS playlist
- `dash` - MPEG-DASH

### ProcessingJobType
- `Transcode` - Video transcoding
- `Thumbnail` - Thumbnail generation
- `Transcription` - AI transcription
- `Analysis` - Video analysis

### ProcessingJobStatus
- `Pending` - Queued for processing
- `Processing` - Currently processing
- `Completed` - Successfully completed
- `Failed` - Processing failed

### TranscriptionStatus
- `Pending` - Awaiting processing
- `Processing` - AI processing in progress
- `Completed` - Transcription complete
- `Failed` - Transcription failed

### PermissionType
- `View` - Can watch video
- `Embed` - Can embed video
- `Download` - Can download video

### PlaylistVisibility
- `Public` - Visible to all
- `Private` - Only visible to owner

### ReactionType
- `Like` - Positive reaction
- `Dislike` - Negative reaction

### AnalyticsEventType
- `Play` - Video started playing
- `Pause` - Video paused
- `Seek` - User seeked to position
- `Complete` - Video watched to end
- `Close` - Video player closed

### NotificationType
- `Comment` - New comment on video
- `Like` - Video received like
- `Upload` - Video upload by followed user
- `ProcessingComplete` - Video processing finished
- `Reply` - Reply to user's comment

---

## Relationships Summary

```
Users (1) -> (N) Videos [UploaderId]
Users (1) -> (N) UploadSessions [UserId]
Users (1) -> (N) Playlists [OwnerId]
Users (1) -> (N) Bookmarks
Users (1) -> (N) VideoReactions
Users (1) -> (N) VideoComments
Users (1) -> (N) Notifications
Users (1) -> (N) AccessControl
Users (1) -> (N) AnalyticsEvents

UploadSessions (1) -> (N) UploadSessionParts [UploadSessionId]
UploadSessions (N) -> (1) Users [UserId]
UploadSessions (N) -> (1) Videos [VideoId, created at upload start]

Videos (1) -> (N) VideoVersions
Videos (1) -> (N) VideoThumbnails
Videos (1) -> (N) VideoProcessingJobs
Videos (1) -> (N) VideoTranscriptions
Videos (1) -> (N) VideoReactions
Videos (1) -> (N) VideoComments
Videos (1) -> (N) Bookmarks
Videos (1) -> (N) AccessControl
Videos (1) -> (N) AnalyticsEvents
Videos (N) -> (1) Categories [CategoryId]
Videos (N) <-> (N) Tags [via VideoTags]
Videos (N) <-> (N) Playlists [via PlaylistVideos]

VideoVersions (1) -> (N) VideoFiles
VideoFiles (N) -> (1) StorageProviders

VideoComments (1) -> (N) VideoComments [ParentCommentId, self-referencing]

Categories (1) -> (N) Categories [ParentCategoryId, hierarchical]
```

---

## Indexing Strategy

### High Priority Indexes (Critical for Performance)
1. `Users.Email` (UNIQUE) - Authentication
2. `Videos.UploaderId` - User's videos
3. `Videos.CreatedAt` - Recent videos
4. `VideoVersions.VideoId` - Version lookup
5. `UploadSessions.UserId` - User upload session lookup
6. `UploadSessions.ExpiresAt` - Expired session cleanup
7. `UploadSessionParts.UploadSessionId + PartNumber` (UNIQUE) - Chunk assembly and duplicate prevention
8. `AnalyticsEvents.VideoId + EventTime` - Analytics queries
9. `VideoComments.VideoId` - Comment display

### Medium Priority Indexes
10. `AccessControl.ShareToken` (UNIQUE) - Token validation
11. `Bookmarks.UserId` - User bookmark ownership
12. `Playlists.OwnerId` - User playlists
13. `VideoReactions.VideoId` - Reaction counts
14. `UploadSessions.Status` - Active/completed/failed session queries

### Composite Indexes
15. `Bookmarks (UserId, VideoId, TimestampSeconds)` - User video bookmark lookup
16. `VideoReactions (UserId, VideoId)` (UNIQUE) - Prevent duplicates
17. `PlaylistVideos (PlaylistId, OrderIndex)` - Ordered playlist display
18. `Notifications (UserId, IsRead)` - Unread notifications
19. `UploadSessionParts (UploadSessionId, PartNumber)` (UNIQUE) - One row per session part number

---

## Performance Considerations

### Denormalized Fields (For Performance)
- `Videos.ViewCount` - Updated via trigger or background job
- `Tags.UsageCount` - Updated on VideoTags insert/delete
- `Playlists.VideoCount` - Updated on PlaylistVideos changes

### Partitioning Recommendations
- `AnalyticsEvents` - Partition by CreatedAt (monthly/quarterly)
- Consider moving to TimescaleDB for time-series data

### Caching Strategy
- Video metadata (Redis)
- User sessions (Redis)
- View counts (Redis with periodic DB sync)
- Thumbnail URLs (CDN + Redis)

---

## Runtime Configuration Reference

Upload behavior depends on application settings such as chunk size, max file size, session expiration, storage provider, and upload-specific rate limits. These runtime settings are documented separately in [appsettings-readme.md](../appsettings-readme.md).

---

## Data Retention Policies

| Table | Retention | Strategy |
|-------|-----------|----------|
| AnalyticsEvents | 2 years | Archive to cold storage, aggregate to summary tables |
| VideoProcessingJobs | 90 days | Delete completed jobs older than 90 days |
| Notifications | 1 year | Hard delete read notifications after 1 year (or add soft-delete columns) |
| UploadSessionParts | Session lifetime | Delete when parent upload session is deleted or cleanup removes expired sessions |
| UploadSessions | 24 hours for incomplete sessions by default | Expire by `ExpiresAt`; retain completed sessions for audit unless cleanup policy removes them |
| VideoFiles | Indefinite | Keep with Videos |
| All others | Indefinite | Keep indefinitely unless table-specific archival policy is defined |

---

## Migration Notes

### Phase 1 (MVP) - Must Implement
- Done: Users, Videos, VideoVersions, VideoFiles, StorageProviders
- Done: UploadSessions, UploadSessionParts
- Done: VideoThumbnails, VideoProcessingJobs
- Done: Categories, Tags, VideoTags
- Done: VideoReactions, VideoComments (split from Feedback)
- Done: Bookmarks, Playlists, PlaylistVideos

### Phase 2 - Post-MVP
- Pending: Upload session cleanup job for expired/incomplete sessions
- Done: UploadSessions require `VideoId` and enforce `UploadSessions.VideoId -> Videos.Id`
- In progress: VideoTranscriptions integration for caption artifacts (`VTT`/`SRT`)
- Pending: AccessControl enhancements (token lifecycle, revocation UX, auditing)
- Pending: Notifications (when notification system ready)
- Pending: AnalyticsEvents (start collecting immediately, analyze later)

---

## Change Log

### v3.0 (June 20, 2026)
- Updated `VideoTranscriptions` to document one row per `(VideoId, Language, Format)` artifact
- Changed transcription uniqueness from per-language to per-language-and-format
- Clarified canonical caption storage paths for `VTT` and `SRT`

### v2.0 (June 2, 2026)
- Added `UploadSessions` table for resumable upload session lifecycle tracking
- Added `UploadSessionParts` table for per-chunk storage and assembly tracking
- Added `UploadSessionStatus` enumeration
- Added upload-session relationships, indexes, and retention guidance
- Moved application configuration notes to `documentation/appsettings-readme.md`
- Updated schema status to reflect implemented upload session handling

### v1.0 (January 22, 2026)
- Initial schema design
- Removed separate Roles table (using enum instead)
- Merged VideoSettings into Videos table
- Split Feedback into VideoReactions and VideoComments
- Added VideoThumbnails table
- Added VideoProcessingJobs table
- Added VideoTranscriptions table
- Added SessionId to AnalyticsEvents
- Enhanced AccessControl with ShareToken
- Added hierarchical Categories support

---

## Next Steps

1. **Add Upload Session Cleanup** for expired or failed incomplete sessions
2. **Add stale Uploading video cleanup** for expired or failed incomplete sessions
3. **Validate Chunk Completion Semantics** so `IsComplete` is marked consistently or removed if not needed
4. **Tune Upload Rate Limits** per deployment based on chunk size and concurrent upload expectations
5. **Document S3 Multipart Flow** when S3 presigned upload implementation is finalized

---

**Schema Status:** Updated for implemented upload session handling and upload configuration







