# Transcriptions Frontend Contract

This document defines the current frontend contract for Stream Forge transcription, caption, transcript-search, and transcription-status behavior.

It is intentionally product-facing rather than implementation-heavy, so frontend work can move without reverse-engineering backend internals.

## Scope

This contract covers:

- transcript reading UX
- caption file selection and download behavior
- transcript-search result behavior
- polling behavior for active transcription states
- multiple transcription artifacts per video
- endpoint-to-UX mapping for current transcription APIs
- current status normalization guidance

It does not yet cover:

- semantic/vector transcript retrieval
- grounded video Q&A
- signed media URLs for caption delivery

## Current Backend Model

The backend currently exposes three practical transcript-related representations:

1. `VTT` and `SRT` caption artifacts through `VideoTranscriptions`
2. persisted transcript chunks in `VideoTranscriptChunks`
3. transcript search results returned from transcript-chunk search

The frontend should treat these as different product surfaces, not as one interchangeable blob.

## Authorization And Download Rules

Caption artifact download/read access currently follows the same authorization rules as video viewing for user-facing endpoints.

That means caption artifact access is allowed for:

- anyone who can watch the video
- including authenticated owner/admin/editor/viewer access where applicable
- including valid `shareToken` access for endpoints that support video-view authorization

It is not limited to:

- authenticated users only
- admin/editor roles only

In product terms, if the viewer is allowed to watch the video, the viewer is also allowed to fetch the user-facing caption artifact for that video.

## Content Formats

### Caption Artifacts

`VTT` and `SRT` are the canonical stored caption artifacts for:

- player caption tracks
- downloads
- external export

These are file-oriented assets, not the preferred primary format for the transcript reading UI.

### Transcript Reading Content

The readable transcript experience should be built from timestamped transcript chunks and transcript-search-style content, not from raw `VTT` or `SRT` parsing unless a deliberate future UI chooses that route.

Recommended frontend assumption:

- transcript panel = timestamp-linked rows
- download/view switcher = caption artifact selection
- player tracks = `VTT` preferred, `SRT` fallback/download

### Plain Text

Plain-text transcript content is not currently treated as a first-class stored artifact.

If a transcript reading view needs plain readable text, it should derive that experience from persisted transcript chunks.

## Exact Endpoint To UX Mapping

This is the current intended frontend mapping:

### `GET /api/v1/videos/{videoId}/transcriptions`

Use for:

- discovering available transcription artifacts
- language selection
- format selection
- status display
- deciding which artifact is the primary default choice

This is the metadata/index endpoint, not the transcript-reader content source.

### `GET /api/v1/videos/{videoId}/transcriptions/{transcriptionId}`

Use for:

- fetching a single transcription artifact payload
- direct file viewing/downloading behavior
- player-caption file access

### `GET /api/v1/videos/{videoId}/transcriptions/{transcriptionId}/content`

Use for:

- the same file-oriented artifact access behavior as the route above

Current implementation note:

- `/content` is not a structured transcript-chunk API
- it is a file/content delivery route for the stored artifact
- in practice this means the frontend should treat it as caption-file retrieval, not as the primary transcript-panel data source

### `GET /api/v1/videos/{videoId}/transcriptions/{transcriptionId}/chunks`

Use for:

- building the main readable transcript panel
- rendering timestamp-linked transcript rows
- loading the structured transcript for a selected transcription artifact

Current implementation note:

- this returns structured transcript chunk rows, not raw file text
- if the requested transcription artifact is a sibling format for the same video/language pair, the backend still resolves the shared transcript chunks
- this is now the primary transcript-reading endpoint for the frontend

### `GET /api/v1/videos/{videoId}/transcript-search?q=...`

Use for:

- readable transcript search results
- timestamp-linked transcript navigation
- matched-row highlighting targets
- chunk-oriented transcript UI foundations

Current recommendation:

- the main transcript panel should be built around chunk-style transcript content, not raw caption-file parsing
- `transcript-search` should be treated as the search/navigation endpoint, not the primary full-transcript read endpoint

### Practical Current Guidance

So, today:

- transcript artifact endpoints = file retrieval
- transcript chunks endpoint = full transcript reading data
- transcript search endpoint = chunk-shaped search/navigation data

## Transcript UI Contract

The frontend should support:

- a readable transcript panel
- timestamp-linked transcript rows
- matched-row highlighting for transcript search results
- caption download actions for completed artifacts

The frontend should not assume that a raw caption file alone is the primary transcript-reading contract.

Because of the current API shape, the frontend should not assume `GET /transcriptions/{transcriptionId}/content` returns transcript rows or structured JSON for transcript rendering.

## Transcript Search Behavior

Current recommended behavior for clicking a transcript-search result:

1. seek the player to the selected timestamp
2. open the transcript panel if it is hidden
3. highlight the matched transcript row/chunk

Default recommendation:

- seek and highlight
- do not auto-play by default

Why this is the product default:

- `seek only` is too weak
- `seek and play` can feel abrupt
- `seek + highlight` gives clear spatial confirmation without forcing playback

If a future UX wants autoplay, that should be a deliberate product decision rather than the default contract.

## What `/content` Returns Today

`GET /api/v1/videos/{videoId}/transcriptions/{transcriptionId}`

and

`GET /api/v1/videos/{videoId}/transcriptions/{transcriptionId}/content`

currently behave as artifact/file delivery endpoints.

Frontend should assume they return:

- the stored transcription artifact stream
- content type based on artifact format
- `text/vtt` for `VTT`
- `application/x-subrip` for `SRT`

Frontend should not assume they return:

- structured JSON chunk rows
- normalized plain-text transcript payloads
- a format-independent transcript rendering shape

So if the frontend needs:

- transcript rows
- row highlighting
- timestamp-linked reading UI

it should use `/transcriptions/{transcriptionId}/chunks`, not `/content`.

## Polling Contract

Pages that should auto-poll while a transcription is active:

- watch page
- manage page
- admin transcription jobs page

Recommended behavior:

- only poll while the status is `Pending` or `Processing`
- stop polling when the status becomes terminal such as `Completed` or `Failed`
- admin jobs can poll a bit more aggressively than watch/manage if needed

Recommended default intervals:

- watch/manage: every 5-10 seconds while active
- admin jobs: every 3-5 seconds while active

The frontend should not keep polling completed or failed jobs indefinitely.

## Status Model And Frontend Normalization

There are two relevant status surfaces in the current backend:

### Persisted Transcription Row Status

The persisted `VideoTranscription` status enum currently uses:

- `Pending`
- `Processing`
- `Completed`
- `Failed`

For normal user-facing transcription row state, the frontend should assume only these four persisted states.

### Aggregated Job Status

Grouped transcription-job views may also emit:

- `Partial`

`Partial` means the grouped artifacts for the same logical job do not all share the same final persisted state.

This is mainly relevant for job-oriented views, especially admin/manage surfaces.

### Live Worker Status

When `LiveStatus` is present, its `status` field is provider/worker-oriented rather than the strict persisted enum.

For the current local worker, frontend should expect values such as:

- `queued`
- `running`
- `completed`
- `failed`

The worker submission response may also use:

- `accepted`

but that is not the main steady-state polling contract for list/status rendering.

### Recommended Frontend Normalization

For UI purposes, normalize current known values into these buckets:

- active:
  - persisted `Pending`
  - persisted `Processing`
  - live `queued`
  - live `running`
- success:
  - persisted `Completed`
  - live `completed`
- failure:
  - persisted `Failed`
  - live `failed`
- mixed:
  - grouped job `Partial`

Current frontend guidance:

- poll while state is active
- stop polling for success/failure/mixed
- render `Partial` as a grouped-job/admin-state concept, not as a normal single-artifact player state

## Multiple Transcriptions Per Video

The backend supports multiple transcription artifacts per video.

That means the frontend should assume:

- multiple languages may exist
- multiple formats may exist
- more than one completed artifact may exist for the same video

The frontend should not assume there is exactly one transcription record for a video.

## Primary Selection Contract

Even though multiple transcription artifacts may exist, the frontend should still derive one primary default selection for display.

Recommended priority order:

1. user-preferred language, if available
2. completed artifacts over non-completed artifacts
3. `VTT` over `SRT` for player-facing use
4. most recently updated artifact if a tie remains

This gives a stable default while still allowing user choice.

## Language And Format UX

Recommended frontend behavior:

- transcript reader: language-selectable
- player captions: language-selectable
- download actions: format-selectable

The UI should present one primary selected transcription by default, while still exposing alternative languages and formats when available.

## Share Token Behavior

User-facing transcription endpoints follow video-view authorization rules.

That means:

- authenticated owner/admin access works
- shared video access via `shareToken` also applies to transcription reads/search where the endpoint supports video-view authorization

Admin-only transcription settings and admin job pages do not use share-token access.

## Current API Usage Guidance

Frontend work should currently think about the API in three groups:

### Status And Artifact Discovery

Use video transcription listing/status endpoints to discover:

- available artifacts
- language
- format
- current processing state
- live in-flight progress where available

### Transcript Reading And Search

Use `/transcriptions/{transcriptionId}/chunks` for:

- readable transcript panels
- timestamp rows
- transcript-panel rendering

Use `transcript-search` for:

- search result navigation
- matched-chunk discovery

Do not use `/transcriptions/{transcriptionId}` or `/content` as if they were structured chunk endpoints for the main transcript reading surface.

### Caption Playback And Download

Use completed transcription artifact endpoints for:

- player caption tracks
- direct downloads

## What The Frontend Should Assume Today

Safe assumptions:

- there may be multiple transcription artifacts per video
- `VTT` is the preferred player-facing artifact when available
- transcript reading should be chunk/timestamp oriented
- search-result clicks should seek and highlight
- watch, manage, and admin jobs pages should poll active states
- anyone allowed to watch the video is also allowed to fetch the user-facing caption artifact
- `/content` is file delivery, not transcript-row JSON
- `/chunks` is the primary structured transcript-reading endpoint

Unsafe assumptions:

- there is always exactly one transcription
- transcript reading should be driven only by raw caption-file parsing
- clicking a search result should always auto-play
- polling is only needed on the watch page
- `/content` returns a normalized transcript-reading payload
- only authenticated or admin users may download captions for a video they are otherwise allowed to watch

## Planned Evolution

This contract is expected to grow later with:

- PostgreSQL full-text search improvements
- semantic/vector retrieval
- grounded Q&A
- signed caption/media URLs

Those additions should extend this contract, not force a totally different frontend flow.
