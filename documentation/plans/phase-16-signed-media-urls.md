# Phase 16 - Signed Media Access URLs

Status: [ ] Planned

## Purpose

Introduce backend-issued signed media access for protected playback resources without coupling clients to the active storage provider or the current authentication transport.

This phase should cover:

- thumbnails
- playback manifests
- HLS playlists and segment assets
- caption/transcription files used by players

The goal is to let a frontend or player fetch protected media through normal URL-based loading such as `<img>`, native `<video>`, HLS.js, and caption tracks, while keeping authorization enforced at signed-URL issuance time.

## Why This Is Deferred

The current backend can already:

- authorize video access through JWT/current-user/share-token rules
- serve local playback assets through API routes
- serve caption files through API routes

That is enough to keep video workflows moving now. Signed media URLs are a product and infrastructure refinement that should be implemented deliberately after the current transcription and caption pipeline settles.

This phase is intentionally separate because it touches:

- public playback contracts
- manifest rewriting behavior
- storage abstraction design
- future S3/object-storage strategy
- future cookie/OIDC compatibility

## Summary

Add a dedicated backend-issued media-access flow built around:

- a `GET /api/v1/videos/{videoId}/media-access` endpoint
- stateless HMAC-signed short-lived media URLs
- support for both current local/API-served media and future provider-presigned storage URLs

Existing content DTOs can keep their current route-shaped media URLs during transition, but the long-term frontend contract for protected playback should become the media-access bundle.

## Key Changes

### 1. Signed URL model

Add a new media signing abstraction for read access, separate from upload targets.

Default design:

- stateless signed URLs
- HMAC-based signature validation
- no DB-backed token records in the first iteration
- payload includes:
  - `videoId`
  - media kind such as `thumbnail`, `manifest`, `asset`, or `caption`
  - canonical asset path or caption resource identity
  - expiry timestamp
  - optional share-token context if needed

Suggested default TTLs:

- thumbnail: 5-15 minutes
- manifest: 1-5 minutes
- playback assets/captions: 5-15 minutes

### 2. Media-access API

Add a dedicated endpoint:

- `GET /api/v1/videos/{videoId}/media-access`

Behavior:

- caller must already be allowed to view the video
- supports authenticated access and share-token access
- returns a media-access bundle containing:
  - `thumbnailUrl`
  - `manifestUrl`
  - `captions`
  - `expiresAt`
  - optional `assetBaseUrl` or similar helper metadata if useful

The media-access endpoint is the place where normal application auth happens. After that, the returned signed URLs can be used by browser media elements without bearer headers.

### 3. Existing delivery routes

Keep the existing routes, but evolve them into delivery/validation endpoints:

- `GET /api/v1/videos/{videoId}/thumbnail`
- `GET /api/v1/videos/{videoId}/playback/manifest`
- `GET /api/v1/videos/{videoId}/playback/assets/{assetPath}`
- caption delivery endpoints under video transcriptions

Add signed-query validation to those routes while keeping current direct authorization behavior temporarily for backward compatibility.

The first implementation should:

- accept a valid signed URL without requiring auth headers
- reject tampered or expired signed URLs
- continue to allow current authenticated/share-token access during migration

### 4. Manifest rewriting

Signed playback does not stop at the master manifest.

Update manifest delivery so that:

- master manifests emit signed URLs for child playlists/assets
- variant `.m3u8` playlists emit signed URLs for segment files
- path normalization is consistent across local storage and future object storage

This is required so one successful signed manifest fetch does not fail on the next playlist or segment request.

### 5. Captions

Caption files should participate in the same media-access model.

The media-access response should include signed caption URLs for completed player-facing artifacts such as:

- `VTT`
- `SRT`

Playback-related caption access should continue to reuse existing video visibility/access rules at issuance time.

### 6. Storage abstraction direction

Extend the storage/media-access abstraction so the application can issue:

- app-signed API URLs for local storage
- provider-presigned read URLs for future S3/object storage

The frontend should not need a different integration flow for local storage versus S3.

### 7. Configuration

Add configuration for signed media delivery, for example:

- `Enabled`
- signing secret
- TTLs per media kind
- local media delivery mode

This should stay compatible with:

- current JWT usage
- future cookie-based auth
- future OAuth/OIDC sign-in

## Test Plan

Add coverage for:

- media-access bundle issuance for:
  - public videos
  - owners
  - admins
  - granted/share-token viewers
  - denied private access
- signed thumbnail access without bearer headers
- signed manifest rewriting for master and variant playlists
- signed asset rejection for:
  - expiry
  - tampering
  - wrong video id
  - wrong media kind/path
- signed caption retrieval
- local storage signed delivery behavior
- abstraction compatibility for future provider-presigned URL implementations

## Assumptions

- No DB schema change is required for the first signed-URL iteration.
- Signed URLs are delivery tokens, not the primary identity model.
- Existing content/search DTOs can remain stable during migration.
- The preferred long-term client flow is:
  1. authenticate normally
  2. request `/media-access`
  3. use returned signed URLs in the player and image tags
- Future S3/object-storage support should fit under the same media-access contract rather than creating a second frontend flow.
