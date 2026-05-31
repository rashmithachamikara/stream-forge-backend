# Stream Forge Database Schema v1.0

**Version:** 1.0  
**Date:** January 22, 2026  
**Status:** Final - Ready for Implementation

---

## Overview

This document defines the complete database schema for Stream Forge, a self-hosted video-on-demand platform. The schema is designed to support:
- Multi-user video management
- Multiple video versions (resolutions, formats)
- Pluggable storage providers
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
| UploaderId | GUID | FK → Users(Id), NOT NULL | User who uploaded the video |
| CategoryId | GUID | FK → Categories(Id), NULL | Video category |
| Visibility | ENUM | NOT NULL | Public, Private, Internal |
| AllowComments | BOOLEAN | NOT NULL, DEFAULT TRUE | Enable comments |
| AllowLikes | BOOLEAN | NOT NULL, DEFAULT TRUE | Enable likes |
| AllowBookmarks | BOOLEAN | NOT NULL, DEFAULT TRUE | Enable bookmarks |
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
- `IX_Videos_CreatedAt`

**Notes:**
- VideoSettings merged into Videos table (1:1 relationship eliminated)
- ViewCount denormalized for performance

---

### 3. VideoVersions

Different resolutions/formats of the same video.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique version identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Parent video |
| Resolution | VARCHAR(20) | NOT NULL | 1080p, 720p, 480p, 360p, 240p |
| Format | VARCHAR(20) | NOT NULL | mp4, webm, hls, dash |
| Bitrate | INT | NULL | Bitrate in kbps |
| Codec | VARCHAR(50) | NULL | Video codec (h264, h265, vp9) |
| StoragePath | VARCHAR(1000) | NOT NULL | Path to video file |
| SizeBytes | BIGINT | NOT NULL | File size in bytes |
| DurationSeconds | INT | NOT NULL | Video duration |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Indexes:**
- `IX_VideoVersions_VideoId`
- `IX_VideoVersions_Resolution`

**Notes:**
- Each video can have multiple versions for adaptive streaming
- StoragePath is relative to StorageProvider configuration

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
| VideoVersionId | GUID | FK → VideoVersions(Id), NOT NULL | Associated video version |
| StorageProviderId | GUID | FK → StorageProviders(Id), NOT NULL | Storage provider |
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

---

### 6. VideoThumbnails

Video thumbnail images.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique thumbnail identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Parent video |
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

### 7. VideoProcessingJobs

Tracks background video processing tasks.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique job identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Video being processed |
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

### 8. VideoTranscriptions

AI-generated video transcriptions and subtitles.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique transcription identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Parent video |
| Language | VARCHAR(10) | NOT NULL | ISO language code (en, es, fr) |
| Format | VARCHAR(10) | NOT NULL | SRT, VTT, TXT |
| StoragePath | VARCHAR(1000) | NOT NULL | Transcription file path |
| Status | ENUM | NOT NULL | Pending, Processing, Completed, Failed |
| Source | VARCHAR(50) | NOT NULL | AI provider (OpenAI, Azure, Manual) |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NULL | Last update timestamp |

**Indexes:**
- `IX_VideoTranscriptions_VideoId`
- `IX_VideoTranscriptions_Language`

**Notes:**
- Supports multiple languages per video
- Status tracks AI processing state

---

### 9. Categories

Video categorization.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique category identifier |
| Name | VARCHAR(100) | NOT NULL, UNIQUE | Category name |
| Description | TEXT | NULL | Category description |
| ParentCategoryId | GUID | FK → Categories(Id), NULL | Parent category (hierarchical) |
| DisplayOrder | INT | NOT NULL, DEFAULT 0 | Display order |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Indexes:**
- `IX_Categories_Name` (UNIQUE)
- `IX_Categories_ParentCategoryId`

**Notes:**
- Supports hierarchical categories
- DisplayOrder for custom sorting

---

### 10. Tags

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

### 11. VideoTags (Join Table)

Many-to-many relationship between Videos and Tags.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| VideoId | GUID | FK → Videos(Id), NOT NULL | Video identifier |
| TagId | GUID | FK → Tags(Id), NOT NULL | Tag identifier |
| CreatedAt | TIMESTAMP | NOT NULL | Association timestamp |

**Primary Key:** Composite (VideoId, TagId)

**Indexes:**
- `IX_VideoTags_VideoId`
- `IX_VideoTags_TagId`

---

### 12. AccessControl

Video access permissions and sharing tokens.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique access control identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Video identifier |
| UserId | GUID | FK → Users(Id), NULL | User (null for token-based) |
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

### 13. Playlists

User-created video playlists.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique playlist identifier |
| Name | VARCHAR(255) | NOT NULL | Playlist name |
| Description | TEXT | NULL | Playlist description |
| OwnerId | GUID | FK → Users(Id), NOT NULL | Playlist owner |
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

### 14. PlaylistVideos (Join Table)

Many-to-many relationship between Playlists and Videos with ordering.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| PlaylistId | GUID | FK → Playlists(Id), NOT NULL | Playlist identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Video identifier |
| OrderIndex | INT | NOT NULL | Display order in playlist |
| AddedAt | TIMESTAMP | NOT NULL | Addition timestamp |

**Primary Key:** Composite (PlaylistId, VideoId)

**Indexes:**
- `IX_PlaylistVideos_PlaylistId_OrderIndex`
- `IX_PlaylistVideos_VideoId`

**Notes:**
- OrderIndex allows custom video ordering in playlists

---

### 15. Bookmarks

User video bookmarks.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique bookmark identifier |
| UserId | GUID | FK → Users(Id), NOT NULL | User identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Video identifier |
| CreatedAt | TIMESTAMP | NOT NULL | Bookmark timestamp |

**Indexes:**
- `IX_Bookmarks_UserId`
- `IX_Bookmarks_VideoId`
- `UQ_Bookmarks_UserId_VideoId` (UNIQUE)

**Notes:**
- Unique constraint prevents duplicate bookmarks

---

### 16. VideoReactions

User likes/dislikes on videos.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique reaction identifier |
| UserId | GUID | FK → Users(Id), NOT NULL | User identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Video identifier |
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

### 17. VideoComments

User comments on videos with reply support.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique comment identifier |
| UserId | GUID | FK → Users(Id), NOT NULL | Commenter |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Video identifier |
| ParentCommentId | GUID | FK → VideoComments(Id), NULL | Parent comment for replies |
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

### 18. Notifications

User notification system.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique notification identifier |
| UserId | GUID | FK → Users(Id), NOT NULL | Recipient user |
| VideoId | GUID | FK → Videos(Id), NULL | Related video |
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

### 19. AnalyticsEvents

Video viewing and interaction analytics.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PK | Unique event identifier |
| VideoId | GUID | FK → Videos(Id), NOT NULL | Video identifier |
| UserId | GUID | FK → Users(Id), NULL | User (null for anonymous) |
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

### StorageProviderType
- `Local` - Local filesystem
- `S3` - AWS S3 or S3-compatible
- `AzureBlob` - Azure Blob Storage

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
Users (1) ──→ (N) Videos [UploaderId]
Users (1) ──→ (N) Playlists [OwnerId]
Users (1) ──→ (N) Bookmarks
Users (1) ──→ (N) VideoReactions
Users (1) ──→ (N) VideoComments
Users (1) ──→ (N) Notifications
Users (1) ──→ (N) AccessControl
Users (1) ──→ (N) AnalyticsEvents

Videos (1) ──→ (N) VideoVersions
Videos (1) ──→ (N) VideoThumbnails
Videos (1) ──→ (N) VideoProcessingJobs
Videos (1) ──→ (N) VideoTranscriptions
Videos (1) ──→ (N) VideoReactions
Videos (1) ──→ (N) VideoComments
Videos (1) ──→ (N) Bookmarks
Videos (1) ──→ (N) AccessControl
Videos (1) ──→ (N) AnalyticsEvents
Videos (N) ──→ (1) Categories [CategoryId]
Videos (N) ←─→ (N) Tags [via VideoTags]
Videos (N) ←─→ (N) Playlists [via PlaylistVideos]

VideoVersions (1) ──→ (N) VideoFiles
VideoFiles (N) ──→ (1) StorageProviders

VideoComments (1) ──→ (N) VideoComments [ParentCommentId, self-referencing]

Categories (1) ──→ (N) Categories [ParentCategoryId, hierarchical]
```

---

## Indexing Strategy

### High Priority Indexes (Critical for Performance)
1. `Users.Email` (UNIQUE) - Authentication
2. `Videos.UploaderId` - User's videos
3. `Videos.CreatedAt` - Recent videos
4. `VideoVersions.VideoId` - Version lookup
5. `AnalyticsEvents.VideoId + EventTime` - Analytics queries
6. `VideoComments.VideoId` - Comment display

### Medium Priority Indexes
7. `AccessControl.ShareToken` (UNIQUE) - Token validation
8. `Bookmarks.UserId` - User bookmarks
9. `Playlists.OwnerId` - User playlists
10. `VideoReactions.VideoId` - Reaction counts

### Composite Indexes
11. `Bookmarks (UserId, VideoId)` (UNIQUE) - Prevent duplicates
12. `VideoReactions (UserId, VideoId)` (UNIQUE) - Prevent duplicates
13. `PlaylistVideos (PlaylistId, OrderIndex)` - Ordered playlist display
14. `Notifications (UserId, IsRead)` - Unread notifications

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

## Data Retention Policies

| Table | Retention | Strategy |
|-------|-----------|----------|
| AnalyticsEvents | 2 years | Archive to cold storage, aggregate to summary tables |
| VideoProcessingJobs | 90 days | Delete completed jobs older than 90 days |
| Notifications | 1 year | Hard delete read notifications after 1 year (or add soft-delete columns) |
| VideoFiles | Indefinite | Keep with Videos |
| All others | Indefinite | Keep indefinitely unless table-specific archival policy is defined |

---

## Migration Notes

### Phase 1 (MVP) - Must Implement
- ✅ Users, Videos, VideoVersions, VideoFiles, StorageProviders
- ✅ VideoThumbnails, VideoProcessingJobs
- ✅ Categories, Tags, VideoTags
- ✅ VideoReactions, VideoComments (split from Feedback)
- ✅ Bookmarks, Playlists, PlaylistVideos

### Phase 2 - Post-MVP
- ⏳ VideoTranscriptions (when AI integration ready)
- ⏳ AccessControl enhancements (token lifecycle, revocation UX, auditing)
- ⏳ Notifications (when notification system ready)
- ⏳ AnalyticsEvents (start collecting immediately, analyze later)

---

## Change Log

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

1. **Implement Domain Entities** based on this schema
2. **Create EF Core Configurations** with Fluent API
3. **Generate Initial Migration**
4. **Seed Test Data**
5. **Create Repository Interfaces**

---

**Schema Status:** ✅ **APPROVED - Ready for Implementation**
