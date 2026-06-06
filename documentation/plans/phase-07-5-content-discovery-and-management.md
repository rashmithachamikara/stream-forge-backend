# Phase 7.5 - Content Discovery And Management

Status: [X] Implemented

## Purpose

Expose the core content APIs needed for frontend browsing, library views, video detail pages, and metadata management. Phase 7.5 sits between playback infrastructure and engagement features: videos can already be uploaded, processed, and played by ID, but clients still need REST APIs to discover and manage content.

## Goals

- Add visibility-aware video list and detail APIs.
- Add owner/library APIs for a user's uploaded videos.
- Add video metadata update and delete/archive APIs.
- Add category, tag, user/profile, and share-management APIs needed by browse, upload, and watch-page flows.
- Add basic search and filtering across ready videos.
- Add consistent pagination for every unbounded collection endpoint.
- Keep controllers thin and move query/update behavior into Application use cases.
- Reuse existing authorization rules for private, internal, public, owner, access-grant, and share-token scenarios.

## Implemented Scope

- Visibility-aware paginated video browse API.
- Video detail API that reuses existing view authorization.
- Authenticated current-user video library API.
- Authenticated current-user upload-session recovery/list API.
- Owner/admin video metadata, visibility, engagement-setting, player-setting, and tag update API.
- Owner/admin video archive/delete API.
- Owner/admin processing-status API.
- Category list/detail and category-video browse APIs.
- Paginated tag search/detail and tag-video browse APIs.
- Public user profile and public user-video browse APIs.
- Admin-only paginated user search API.
- Owner/admin video access grant and share-token list/create/revoke APIs.
- Shared paginated response DTO for unbounded collection endpoints.
- Repository-backed pagination and deterministic ordering for list APIs.

## Planned API Surface

### Videos

- `GET /api/v1/videos` for public/internal browse and filtered discovery. Paginated.
- `GET /api/v1/videos/{videoId}` for video detail metadata.
- `GET /api/v1/videos/{videoId}/processing-status` for owner/admin upload and processing progress views.
- `PATCH /api/v1/videos/{videoId}` for owner/admin metadata, visibility, category, tags, and player-setting updates.
- `DELETE /api/v1/videos/{videoId}` or `POST /api/v1/videos/{videoId}/archive` for owner/admin removal workflows.

### Current User Library

- `GET /api/v1/me/videos` for the authenticated user's uploaded video library. Paginated.
- `GET /api/v1/me/upload-sessions` for active/recent upload sessions if the frontend needs resumable upload recovery. Paginated.
- `GET /api/v1/me/bookmarks` belongs to Phase 8, but should be paginated when implemented.
- `GET /api/v1/me/playlists` belongs to Phase 8, but should be paginated when implemented.

### Categories

- `GET /api/v1/categories` for category pickers and browsing. Unpaginated by default for the curated category tree.
- `GET /api/v1/categories/{categoryId}` for category details.
- `GET /api/v1/categories/{categoryId}/videos` for category-specific browse pages. Paginated.

### Tags

- `GET /api/v1/tags` for tag search/autocomplete and filtering. Paginated or limited.
- `GET /api/v1/tags/{tagId}` for tag details if tag pages are needed.
- `GET /api/v1/tags/{tagId}/videos` for tag-specific browse pages. Paginated.

### Users And Profiles

- `GET /api/v1/users/{userId}` for public profile metadata if creator pages require it.
- `GET /api/v1/users/{userId}/videos` for public creator pages. Paginated.
- `GET /api/v1/users` for admin/user-management search if admin screens require it. Paginated and admin-only.

### Sharing And Access

- `GET /api/v1/videos/{videoId}/access` for owner/admin access grants and share links. Paginated with optional `isActive` filter.
- `POST /api/v1/videos/{videoId}/access` to grant user access or create a share token.
- `DELETE /api/v1/videos/{videoId}/access/{accessControlId}` to revoke a grant or share token.

### Existing Playback Endpoints From Phase 6

- `GET /api/v1/videos/{videoId}/playback/manifest`.
- `GET /api/v1/videos/{videoId}/playback/assets/{assetPath}`.
- `GET /api/v1/videos/{videoId}/thumbnail`.
- These endpoints are file-serving endpoints and should not be paginated.

## Query And Filtering Requirements

- Support paging with deterministic ordering.
- Use offset pagination for the first implementation: `page`, `pageSize`, `totalCount`, `totalPages`, `hasNextPage`, and `hasPreviousPage`.
- Cap `pageSize` with a server-side maximum, such as `100`.
- Support filters for category, tag, owner, status, visibility where appropriate.
- Support text search over video title and description for `GET /api/v1/videos`; PostgreSQL `ILIKE` is acceptable for the first version.
- Support text search over tag name for `GET /api/v1/tags`.
- Support admin/user search by name and email for `GET /api/v1/users` if that endpoint is implemented.
- Use deterministic ordering for every paginated endpoint, such as `CreatedAt desc, Id desc` or `Name asc, Id asc`.
- Default public browse APIs should only return `Ready` and visible videos.
- Authenticated library APIs may return the owner's `Uploading`, `Processing`, `Ready`, and `Failed` videos.
- Admin APIs may include `Deleted` videos when explicitly requested.
- Avoid exposing storage paths or internal processing details in public DTOs.
- Include thumbnail and playback URLs only as API route URLs, not filesystem paths.

## Pagination Requirements

- Pagination is required for `GET /api/v1/videos`.
- Pagination is required for `GET /api/v1/me/videos`.
- Pagination is required for `GET /api/v1/me/upload-sessions` if implemented.
- Pagination is required for `GET /api/v1/categories/{categoryId}/videos`.
- Pagination is required for `GET /api/v1/tags`.
- Pagination is required for `GET /api/v1/tags/{tagId}/videos`.
- Pagination is required for `GET /api/v1/users/{userId}/videos`.
- Pagination is required for `GET /api/v1/users` if implemented.
- Pagination is required for `GET /api/v1/videos/{videoId}/access` if user-level grants are listed.
- Pagination is not required for single-resource detail endpoints.
- Pagination is not required for `GET /api/v1/categories` while categories are a curated, bounded tree.

## Clean Architecture Design

- API layer binds route/query/body values and returns DTOs.
- Application layer owns list/detail/update/delete use cases and authorization checks.
- Domain layer owns video metadata rules and lifecycle behavior.
- Infrastructure layer owns EF query implementations, repository methods, and persistence details.
- DTOs should be explicit per use case; avoid returning domain entities directly.
- Shared pagination request/response DTOs should live in Application and be reused across list use cases.
- Content use cases must depend on the internal `User.Id` exposed through `ICurrentUserService`, not email addresses, external provider IDs, OIDC issuer values, or provider-specific claims.
- Keep Phase 7.5 compatible with later external identity provider integration: external identities should resolve to local StreamForge users, then StreamForge authorization rules apply.

## Dependencies

- Phase 4 authorization service and current-user abstractions.
- Future OAuth/OIDC integration should remain decoupled from these APIs; content use cases should not depend on provider-specific identity details.
- Phase 5 upload-created videos and metadata.
- Phase 6 ready/playable videos, thumbnails, and playback routes.
- Existing repositories for videos, users, categories, and tags may need query-specific methods.

## Acceptance Criteria

- Clients can list videos for browse/discovery without knowing video IDs ahead of time.
- Clients can fetch a video detail payload suitable for a watch page.
- Authenticated users can list their own uploaded videos, including non-ready statuses.
- Every unbounded collection endpoint returns a paginated response with deterministic ordering.
- Owners or admins can update allowed video metadata.
- Owners or admins can delete/archive videos according to the chosen product policy.
- Category and tag APIs support upload forms and filtering.
- Creator/profile pages can fetch public profile metadata and public ready videos if that product surface is enabled.
- Owners or admins can create, list, and revoke video access grants or share tokens if sharing management is included in this phase.
- Visibility and access-control rules are enforced consistently.
- API responses do not leak filesystem storage paths or private processing internals.
- Controllers remain thin HTTP adapters.
- The project builds successfully after implementation.

## Notes

- Engagement actions such as likes, comments, bookmarks, playlists, and notifications remain Phase 8.
- Analytics events, watch time, dashboards, and reports remain Phase 9.
- Advanced moderation, recommendations, and full-text search engines can be split into later phases if they outgrow simple database-backed filtering.
