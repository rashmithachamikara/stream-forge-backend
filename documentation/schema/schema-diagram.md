# Stream Forge Entity Relationship Diagram

## Core Entity Relationships

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            STREAM FORGE DATABASE                            │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────┐
│    Users     │
│──────────────│
│ Id (PK)      │──┐
│ Name         │  │
│ Email        │  │
│ PasswordHash │  │
│ Role         │  │
│ IsActive     │  │
└──────────────┘  │
                  │
                  │ 1:N (UploaderId)
                  │
                  ├──────────────────────────────────────────────────────────┐
                  │                                                          │
                  ▼                                                          │
        ┌─────────────────┐                                                 │
        │     Videos      │                                                 │
        │─────────────────│                                                 │
        │ Id (PK)         │──┐                                             │
        │ Title           │  │                                             │
        │ Description     │  │                                             │
        │ UploaderId (FK) │  │ 1:N                                         │
        │ CategoryId (FK) │  │                                             │
        │ Visibility      │  ├─────► ┌──────────────────┐                 │
        │ AllowComments   │  │       │ VideoVersions    │                 │
        │ Autoplay        │  │       │──────────────────│                 │
        │ ViewCount       │  │       │ Id (PK)          │──┐              │
        └─────────────────┘  │       │ VideoId (FK)     │  │              │
              │              │       │ Resolution       │  │ 1:N          │
              │              │       │ Format           │  │              │
              │ 1:N          │       │ StoragePath      │  │              │
              │              │       │ SizeBytes        │  ├───► ┌────────────────┐
              │              │       └──────────────────┘  │     │  VideoFiles    │
              │              │                             │     │────────────────│
              │              │                             │     │ Id (PK)        │
              │              │                             └────►│ VideoVersionId │
              │              │                                   │ StorageProvId  │
              │              │                                   │ FilePath       │
              │              │       ┌──────────────────┐        └────────────────┘
              │              ├─────► │ VideoThumbnails  │               │
              │              │       │──────────────────│               │ N:1
              │              │       │ Id (PK)          │               │
              │              │       │ VideoId (FK)     │               ▼
              │              │       │ StoragePath      │     ┌─────────────────┐
              │              │       │ IsDefault        │     │StorageProviders │
              │              │       └──────────────────┘     │─────────────────│
              │              │                                │ Id (PK)         │
              │              │       ┌─────────────────────┐  │ Name            │
              │              ├─────► │VideoProcessingJobs  │  │ Type            │
              │              │       │─────────────────────│  │ Configuration   │
              │              │       │ Id (PK)             │  │ IsDefault       │
              │              │       │ VideoId (FK)        │  └─────────────────┘
              │              │       │ JobType             │
              │              │       │ Status              │
              │              │       │ Progress            │
              │              │       └─────────────────────┘
              │              │
              │              │       ┌──────────────────────┐
              │              └─────► │VideoTranscriptions   │
              │                      │──────────────────────│
              │                      │ Id (PK)              │
              │                      │ VideoId (FK)         │
              │                      │ Language             │
              │                      │ Format               │
              │                      │ Status               │
              │                      └──────────────────────┘
              │
              │ N:1
              │
              ▼
    ┌───────────────┐
    │  Categories   │
    │───────────────│
    │ Id (PK)       │◄──┐ Self-referencing (hierarchical)
    │ Name          │   │
    │ ParentCatId   │───┘
    └───────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                    TAGGING (Many-to-Many)                       │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐           ┌──────────────┐           ┌──────────┐
│   Videos     │           │  VideoTags   │           │   Tags   │
│──────────────│           │──────────────│           │──────────│
│ Id (PK)      │◄─────────►│ VideoId (PK) │◄─────────►│ Id (PK)  │
└──────────────┘     N:N   │ TagId (PK)   │     N:N   │ Name     │
                            └──────────────┘           └──────────┘


┌─────────────────────────────────────────────────────────────────┐
│                    ENGAGEMENT FEATURES                          │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐                                    ┌──────────────┐
│    Users     │                                    │   Videos     │
│──────────────│                                    │──────────────│
│ Id (PK)      │──┐                            ┌───►│ Id (PK)      │
└──────────────┘  │                            │    └──────────────┘
                  │ 1:N                        │
                  │                            │ N:1
                  ├──────────► ┌─────────────────────┐
                  │            │  VideoReactions     │
                  │            │─────────────────────│
                  │            │ Id (PK)             │
                  │            │ UserId (FK)         │
                  │            │ VideoId (FK)        │
                  │            │ ReactionType        │
                  │            └─────────────────────┘
                  │
                  │            ┌─────────────────────┐
                  ├──────────► │  VideoComments      │
                  │            │─────────────────────│
                  │            │ Id (PK)             │◄──┐ Self-referencing
                  │            │ UserId (FK)         │   │ (replies)
                  │            │ VideoId (FK)        │   │
                  │            │ ParentCommentId (FK)│───┘
                  │            │ Comment             │
                  │            └─────────────────────┘
                  │
                  │            ┌─────────────────────┐
                  ├──────────► │     Bookmarks       │
                  │            │─────────────────────│
                  │            │ Id (PK)             │
                  │            │ UserId (FK)         │
                  │            │ VideoId (FK)        │
                  │            └─────────────────────┘
                  │
                  │            ┌─────────────────────┐
                  └──────────► │   Notifications     │
                               │─────────────────────│
                               │ Id (PK)             │
                               │ UserId (FK)         │
                               │ VideoId (FK)        │
                               │ NotificationType    │
                               │ IsRead              │
                               └─────────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                    PLAYLISTS (Many-to-Many)                     │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐           ┌────────────────┐           ┌──────────────┐
│    Users     │    1:N    │   Playlists    │           │   Videos     │
│──────────────│◄──────────│────────────────│           │──────────────│
│ Id (PK)      │           │ Id (PK)        │◄─────────►│ Id (PK)      │
└──────────────┘           │ OwnerId (FK)   │     N:N   └──────────────┘
                           │ Name           │              ▲
                           │ Visibility     │              │
                           └────────────────┘              │
                                  ▲                        │
                                  │                        │
                                  │ N:N                    │
                                  │                        │
                                  └────────┬───────────────┘
                                           │
                                 ┌─────────────────┐
                                 │PlaylistVideos   │
                                 │─────────────────│
                                 │ PlaylistId (PK) │
                                 │ VideoId (PK)    │
                                 │ OrderIndex      │
                                 └─────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                      ACCESS CONTROL                             │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐           ┌──────────────────┐           ┌──────────────┐
│    Users     │           │  AccessControl   │           │   Videos     │
│──────────────│           │──────────────────│           │──────────────│
│ Id (PK)      │◄─────────►│ Id (PK)          │◄─────────►│ Id (PK)      │
└──────────────┘    N:1    │ UserId (FK)      │    N:1    └──────────────┘
                            │ VideoId (FK)     │
                            │ ShareToken       │
                            │ PermissionType   │
                            │ ExpiresAt        │
                            └──────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                         ANALYTICS                               │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐           ┌──────────────────┐           ┌──────────────┐
│    Users     │           │ AnalyticsEvents  │           │   Videos     │
│──────────────│           │──────────────────│           │──────────────│
│ Id (PK)      │◄─────────►│ Id (PK)          │◄─────────►│ Id (PK)      │
└──────────────┘    N:1    │ UserId (FK)      │    N:1    └──────────────┘
              (nullable)   │ VideoId (FK)     │
                            │ SessionId        │
                            │ EventType        │
                            │ Position         │
                            │ DurationWatched  │
                            └──────────────────┘
```

## Simplified Overview

```
┌────────┐
│ Users  │
└───┬────┘
    │
    ├──► Videos ──┬──► VideoVersions ──► VideoFiles ──► StorageProviders
    │             ├──► VideoThumbnails
    │             ├──► VideoProcessingJobs
    │             ├──► VideoTranscriptions
    │             ├──► VideoReactions
    │             ├──► VideoComments
    │             ├──► Bookmarks
    │             ├──► AccessControl
    │             ├──► AnalyticsEvents
    │             ├──► Categories
    │             └──► Tags (via VideoTags)
    │
    ├──► Playlists ──► Videos (via PlaylistVideos)
    ├──► Bookmarks
    ├──► VideoReactions
    ├──► VideoComments
    ├──► Notifications
    └──► AnalyticsEvents
```

## Key Relationships by Feature

### Video Upload Flow
```
User → Videos → VideoProcessingJobs → VideoVersions → VideoFiles → StorageProviders
                                   └─► VideoThumbnails
```

### Video Playback Flow
```
User → Videos → VideoVersions → VideoFiles → StorageProviders
             └─► AnalyticsEvents (tracking)
```

### Engagement Flow
```
User → Videos → VideoReactions (Like/Dislike)
             └─► VideoComments (with replies)
             └─► Bookmarks
```

### Playlist Flow
```
User → Playlists ←─[PlaylistVideos]─→ Videos
```

### Sharing Flow
```
Videos → AccessControl (ShareToken) → Public Access
```

## Database Cardinality Summary

| Relationship | Type | Description |
|--------------|------|-------------|
| Users → Videos | 1:N | One user uploads many videos |
| Videos → VideoVersions | 1:N | One video has many versions |
| VideoVersions → VideoFiles | 1:N | One version can have multiple storage copies |
| VideoFiles → StorageProviders | N:1 | Many files use one provider |
| Videos → Categories | N:1 | Many videos in one category |
| Videos ↔ Tags | N:N | Videos can have many tags |
| Users ↔ Videos (Reactions) | N:N | Users can react to many videos |
| Users ↔ Videos (Comments) | N:N | Users can comment on many videos |
| Users ↔ Videos (Bookmarks) | N:N | Users can bookmark many videos |
| Users → Playlists | 1:N | One user has many playlists |
| Playlists ↔ Videos | N:N | Playlists contain many videos |
| VideoComments → VideoComments | 1:N | Comments can have replies (self-ref) |
| Categories → Categories | 1:N | Hierarchical categories (self-ref) |

## Cascade Delete Behavior

| Parent | Child | On Delete |
|--------|-------|-----------|
| Users | Videos | SET NULL or RESTRICT (configure) |
| Videos | VideoVersions | CASCADE |
| Videos | VideoThumbnails | CASCADE |
| Videos | VideoProcessingJobs | CASCADE |
| Videos | VideoComments | CASCADE |
| Videos | VideoReactions | CASCADE |
| Videos | Bookmarks | CASCADE |
| VideoVersions | VideoFiles | CASCADE |
| Playlists | PlaylistVideos | CASCADE |
| Users | Playlists | CASCADE |
| Users | Bookmarks | CASCADE |

---

**Note:** This diagram represents the final schema v1.0. All relationships follow Clean Architecture principles with proper foreign key constraints and cascading rules.
