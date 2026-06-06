# Phase 8 - Engagement Features

Status: [X] Implemented

## Purpose

Add first-class engagement APIs for video reactions, comments, bookmarks, playlists, and in-app notifications. Phase 8 builds on Phase 7.5 content discovery and must reuse existing video visibility/access checks, current-user resolution, and pagination patterns.

## Strategy

- Implement in-app engagement through REST APIs only.
- Persist notifications in the database; do not add email, SignalR, web push, or live delivery in this phase.
- Use hard deletes for comments, playlists, playlist videos, reactions, and bookmarks where delete endpoints exist.
- Keep controllers thin and place authorization, validation, notification creation, duplicate handling, and mapping in Application use cases.
- Reuse `PagedQueryResult<T>` and `PagedResponseDto<T>` for unbounded collection endpoints.

## Implemented Scope

- Reaction summary, set, and remove APIs.
- Comment list, create, update, delete, and reply APIs.
- Current-user bookmark list plus bookmark and unbookmark APIs.
- Public and current-user playlist APIs, playlist detail, add/remove video, and reorder endpoints.
- Current-user notification list, unread count, mark-read, mark-unread, and mark-all-read endpoints.
- Notification creation for top-level comments, replies, and likes.
- New repositories and use cases for reactions, comments, bookmarks, playlist videos, and notification pagination.

## Planned API Surface

### Reactions

- `GET /api/v1/videos/{videoId}/reactions/summary`
- `PUT /api/v1/videos/{videoId}/reaction`
- `DELETE /api/v1/videos/{videoId}/reaction`

### Comments

- `GET /api/v1/videos/{videoId}/comments?parentCommentId=&page=&pageSize=`
- `POST /api/v1/videos/{videoId}/comments`
- `PATCH /api/v1/videos/{videoId}/comments/{commentId}`
- `DELETE /api/v1/videos/{videoId}/comments/{commentId}`

### Bookmarks

- `GET /api/v1/me/bookmarks?page=&pageSize=`
- `PUT /api/v1/videos/{videoId}/bookmark`
- `DELETE /api/v1/videos/{videoId}/bookmark`

### Playlists

- `GET /api/v1/playlists?ownerId=&page=&pageSize=`
- `GET /api/v1/me/playlists?page=&pageSize=`
- `POST /api/v1/playlists`
- `GET /api/v1/playlists/{playlistId}`
- `PATCH /api/v1/playlists/{playlistId}`
- `DELETE /api/v1/playlists/{playlistId}`
- `GET /api/v1/playlists/{playlistId}/videos?page=&pageSize=`
- `POST /api/v1/playlists/{playlistId}/videos`
- `DELETE /api/v1/playlists/{playlistId}/videos/{videoId}`
- `POST /api/v1/playlists/{playlistId}/videos/reorder`

### Notifications

- `GET /api/v1/me/notifications?isRead=&page=&pageSize=`
- `GET /api/v1/me/notifications/unread-count`
- `POST /api/v1/me/notifications/{notificationId}/read`
- `POST /api/v1/me/notifications/{notificationId}/unread`
- `POST /api/v1/me/notifications/mark-all-read`

## Behavior Rules

- All create, update, and delete engagement actions require authentication.
- Public read endpoints may be anonymous only when the target video or playlist is viewable by the caller.
- Reactions, comments, and bookmarks require the target video to be `Ready` and viewable.
- `Video.AllowLikes` blocks reaction create/update.
- `Video.AllowComments` blocks new comments and replies.
- `Video.AllowBookmarks` blocks bookmark creation.
- One reaction is allowed per user/video. `PUT` creates or updates the reaction; `DELETE` removes it.
- One bookmark is allowed per user/video. `PUT` is idempotent; `DELETE` is idempotent.
- Comments support top-level comments and replies through `ParentCommentId`.
- Comment update/delete is allowed for the comment author or admin.
- Public playlists are readable by anyone; private playlists are readable by owner/admin only.
- Playlist create/update/delete and playlist video mutation are allowed for playlist owner or admin.
- Adding a video to a playlist requires view access to the target video.
- Playlist add appends by default unless `orderIndex` is supplied.
- Playlist reorder accepts the complete ordered list of video IDs currently in the playlist.
- Notifications are visible only to the recipient user.

## Notification Rules

- New top-level comment notifies the video owner unless the commenter is the owner.
- New reply notifies the parent comment author unless the replier is the same user.
- New like notifies the video owner unless the liker is the owner.
- Dislikes do not create notifications in the first implementation.
- Bookmark and playlist actions do not create notifications in the first implementation.
- Notification creation should happen in the same Application use case as the triggering action.
- Avoid duplicate notifications for the same recipient in a single use case.

## Clean Architecture Design

- API layer binds route/query/body values and returns DTOs.
- Application layer owns engagement orchestration, authorization checks, pagination, mapping, and notification creation.
- Domain layer owns entity invariants such as empty comment prevention, playlist count updates, and reaction type changes.
- Infrastructure layer owns EF query implementations and persistence.
- Do not return domain entities directly from controllers.

## Repository And Use Case Work

- Add repository interfaces and implementations for `VideoReaction`, `VideoComment`, `Bookmark`, and `PlaylistVideo`.
- Extend `IPlaylistRepository` with paginated owner/public/detail queries and playlist-video loading.
- Extend `INotificationRepository` with paginated list, `isRead` filtering, unread count, and current-user mark-read helpers.
- Add Application DTOs grouped by reactions, comments, bookmarks, playlists, and notifications.
- Register new use cases in API dependency injection.

## Dependencies

- Phase 4 authentication/current-user abstractions.
- Phase 4 video authorization service for view/manage checks.
- Phase 7.5 video detail/list patterns and shared pagination DTOs.
- Existing schema tables for `VideoReactions`, `VideoComments`, `Bookmarks`, `Playlists`, `PlaylistVideos`, and `Notifications`.

## Acceptance Criteria

- Users can like, dislike, change, and remove their reaction to a viewable video.
- Users can list reaction counts and their own reaction state for a video.
- Users can create, list, edit, and delete comments and replies where allowed.
- Users can bookmark and unbookmark viewable videos idempotently.
- Users can list their bookmarks with pagination.
- Users can create, list, view, update, and delete playlists.
- Users can add, remove, list, and reorder playlist videos.
- Public/private playlist visibility is enforced.
- In-app notifications are created for comment, reply, and like events.
- Users can list notifications, filter by read state, get unread count, and mark notifications read/unread.
- All unbounded list endpoints are paginated with deterministic ordering.
- The project builds successfully after implementation.

## Test Plan

- Build with `dotnet build StreamForge.sln --no-restore`; if the API output DLLs are locked by a running server, build to a separate verification output directory.
- Test reaction create, update, delete, summary counts, duplicate handling, and `AllowLikes=false`.
- Test comment list pagination, top-level comments, replies, edit/delete authorization, and `AllowComments=false`.
- Test bookmark idempotent create/delete, paginated current-user bookmark list, and `AllowBookmarks=false`.
- Test playlist create/update/delete, public/private visibility, add/remove/reorder videos, and non-owner mutation rejection.
- Test notification creation for comments, replies, and likes.
- Test notification list pagination, `isRead` filter, unread count, mark-read, mark-unread, and mark-all-read.

## Notes

- Do not add email, SignalR, push notifications, moderation queues, reports, anti-spam features, or soft deletes in this phase.
- Do not add analytics event tracking here; watch events and engagement analytics remain Phase 9.
- If implementation discovers missing repository indexes or constraints, document the migration need before adding schema changes.
