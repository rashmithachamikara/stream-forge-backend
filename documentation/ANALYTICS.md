# Analytics

**Date:** June 7, 2026

This document describes the current Stream Forge analytics model, what the backend expects from playback clients, and how frontend apps should integrate with the analytics API.

## Overview

Stream Forge currently uses an event-based analytics model for video playback.

- Playback analytics are recorded through `POST /api/v1/videos/{videoId}/analytics/events`.
- Views are derived from client-sent playback events, not from HLS manifest or segment fetches.
- The backend enriches events with authenticated user identity when available, and optionally with IP address and user agent depending on configuration.
- Analytics reporting is split across:
  - per-video owner analytics
  - current-user portfolio analytics
  - admin platform analytics

This is meant to power product and dashboard analytics. It is not yet a separate warehouse-style analytics pipeline.

## Important Design Choice

Views are not counted from playback asset requests.

That means:

- `GET /api/v1/videos/{videoId}/playback/manifest`
- `GET /api/v1/videos/{videoId}/playback/assets/{assetPath}`

do not count as views by themselves.

Instead, the playback client must send analytics events explicitly. This gives better watch-behavior data because segment requests alone cannot reliably tell whether a viewer actually watched, paused, scrubbed, retried, or abandoned playback.

## Event Ingestion

### Endpoint

`POST /api/v1/videos/{videoId}/analytics/events`

This endpoint is `AllowAnonymous`, but the request still has to pass normal video access rules.

### Request Shape

```json
{
  "sessionId": "4a49ca46-6c0d-4701-b1fc-5c3285c6b0d2",
  "eventType": "Play",
  "eventTime": "2026-06-07T08:45:00Z",
  "position": 12,
  "durationWatched": 15
}
```

Fields:

- `sessionId`: client-generated playback session identifier
- `eventType`: one of `Play`, `Pause`, `Seek`, `Complete`, `Close`
- `eventTime`: optional event timestamp; if omitted, the server still stores the event
- `position`: optional playback position in seconds
- `durationWatched`: optional watched time contribution in seconds

### Response Shape

```json
{
  "eventRecorded": true,
  "viewCountIncremented": false
}
```

Notes:

- `eventRecorded` can be `false` when collection is disabled, for example when `CollectRawEvents=false` or anonymous event collection is turned off.
- `viewCountIncremented` is `true` only when that event caused the video's denormalized view count to cross the configured counted-view threshold.

## How Views Are Counted

The backend does not count every event as a view.

A counted view is currently based on:

- one session per `VideoId + SessionId`
- at least one `Play` event
- accumulated watch time reaching `MinimumViewWatchSeconds`

Default threshold:

- `MinimumViewWatchSeconds = 30`

In practice, the write path works like this:

1. The backend checks how much watch time already exists for the given `VideoId + SessionId`.
2. When a `Play` event arrives with additional watched time, the backend checks whether that session crosses the configured threshold.
3. If the threshold is crossed for the first time, the backend increments `Videos.ViewCount`.

This means analytics summaries and ranked views are based on thresholded playback, not just raw event counts.

## What The Backend Stores

When ingestion is enabled and raw event collection is enabled, the backend stores:

- `VideoId`
- `SessionId`
- `EventType`
- `EventTime`
- `Position`
- `DurationWatched`
- `UserId` when the viewer is authenticated
- `IpAddress` when `CollectIpAddress=true`
- `UserAgent` when `CollectUserAgent=true`

The backend does not currently use playback manifest or segment requests as analytics input.

## Supported Metrics

### Playback Metrics

- total views
- unique viewers
- total watch time
- average watch time
- completion count
- average completion rate
- views over time
- active viewers
- peak watch time by hour-of-day

### Engagement Metrics

- like count
- dislike count
- comment count
- engagement score
- engagement rate
- most liked videos
- most commented videos
- most engaged videos
- most watched videos

### Segmented Metrics

- auth vs anon
- device breakdown
- browser family breakdown
- category breakdown
- tag breakdown

## Configuration

Analytics behavior is controlled by the `Analytics` section in appsettings.

Current options are defined in [AnalyticsOptions.cs](/c:/Files/Shared/Software%20Projects/Stream%20Forge/stream-forge-backend/src/StreamForge.Application/Common/AnalyticsOptions.cs).

### Example

```json
"Analytics": {
  "Enabled": true,
  "IngestionEnabled": true,
  "ReportingEnabled": true,
  "AdminReportingEnabled": true,
  "CollectRawEvents": true,
  "CollectAnonymousEvents": true,
  "CollectUserAgent": true,
  "CollectIpAddress": true,
  "CollectPauseEvents": true,
  "CollectSeekEvents": true,
  "CollectCloseEvents": true,
  "EnableDeviceBreakdown": true,
  "EnableBrowserBreakdown": true,
  "EnableActiveViewerMetrics": true,
  "EnablePeakWatchTimeMetrics": true,
  "MinimumViewWatchSeconds": 30,
  "ActiveViewerWindowMinutes": 5
}
```

### Coarse Toggles

- `Enabled`: master switch for analytics
- `IngestionEnabled`: disables playback event ingestion
- `ReportingEnabled`: disables owner/current-user analytics endpoints
- `AdminReportingEnabled`: disables admin analytics endpoints

### Collection Toggles

- `CollectRawEvents`: if `false`, playback events are not persisted
- `CollectAnonymousEvents`: if `false`, anonymous viewers are ignored by ingestion
- `CollectUserAgent`: controls user-agent persistence
- `CollectIpAddress`: controls IP persistence
- `CollectPauseEvents`: if `false`, `Pause` events are rejected
- `CollectSeekEvents`: if `false`, `Seek` events are rejected
- `CollectCloseEvents`: if `false`, `Close` events are rejected

### Metric Toggles

- `EnableDeviceBreakdown`
- `EnableBrowserBreakdown`
- `EnableActiveViewerMetrics`
- `EnablePeakWatchTimeMetrics`

### Behavior Settings

- `MinimumViewWatchSeconds`: counted-view threshold
- `ActiveViewerWindowMinutes`: recent-activity window for active viewer metrics

## Authorization Rules

### Ingestion

Playback event ingestion may be:

- authenticated
- anonymous

Anonymous ingestion still depends on:

- `CollectAnonymousEvents=true`
- the viewer being allowed to watch the target video

### Reporting

- `GET /api/v1/videos/{videoId}/analytics/*` requires a user who can manage the video
- `GET /api/v1/me/analytics/*` requires authentication
- `GET /api/v1/admin/analytics/*` requires the `Admin` role

## API Surface

### Video Analytics

- `POST /api/v1/videos/{videoId}/analytics/events`
- `GET /api/v1/videos/{videoId}/analytics/summary`
- `GET /api/v1/videos/{videoId}/analytics/timeseries?from=&to=`
- `GET /api/v1/videos/{videoId}/analytics/engagement?from=&to=`

### Current User Analytics

- `GET /api/v1/me/analytics/summary?from=&to=`
- `GET /api/v1/me/analytics/top-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/me/analytics/most-liked-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/me/analytics/most-commented-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/me/analytics/most-engaged-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/me/analytics/views-over-time?from=&to=`
- `GET /api/v1/me/analytics/device-breakdown?from=&to=`
- `GET /api/v1/me/analytics/browser-breakdown?from=&to=`
- `GET /api/v1/me/analytics/auth-breakdown?from=&to=`
- `GET /api/v1/me/analytics/category-breakdown?from=&to=`
- `GET /api/v1/me/analytics/tag-breakdown?from=&to=`
- `GET /api/v1/me/analytics/reports/videos?from=&to=&format=csv`

### Admin Analytics

- `GET /api/v1/admin/analytics/summary?from=&to=`
- `GET /api/v1/admin/analytics/views-over-time?from=&to=`
- `GET /api/v1/admin/analytics/most-watched-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/admin/analytics/most-liked-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/admin/analytics/most-commented-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/admin/analytics/most-engaged-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/admin/analytics/active-viewers`
- `GET /api/v1/admin/analytics/peak-watch-time?from=&to=`
- `GET /api/v1/admin/analytics/device-breakdown?from=&to=`
- `GET /api/v1/admin/analytics/browser-breakdown?from=&to=`
- `GET /api/v1/admin/analytics/auth-breakdown?from=&to=`
- `GET /api/v1/admin/analytics/category-breakdown?from=&to=`
- `GET /api/v1/admin/analytics/tag-breakdown?from=&to=`
- `GET /api/v1/admin/analytics/reports/overview?from=&to=&format=csv`

## Date Range And Pagination Defaults

For reporting endpoints:

- default date range is the last 30 days when `from` and `to` are omitted
- `page` defaults to `1`
- `pageSize` defaults to `24`
- `pageSize` is capped at `100`

## Frontend Integration Guide

This section is meant as a small guide for a frontend agent integrating playback analytics.

### Core Rule

The player should send analytics events over HTTP `POST`, not SignalR.

Why:

- simpler integration
- better retry behavior
- works across reloads and short-lived sessions
- aligns with the backend ingestion model already implemented

### What The Frontend Should Generate

Each playback attempt should have a stable `sessionId`.

Recommended rule:

- create one `sessionId` when a viewer starts a playback session for a video
- keep using that same `sessionId` until the player is destroyed or the user meaningfully starts a fresh watch session

Use a UUID on the client.

### Minimum Events To Send

At a minimum, send:

- `Play`
- `Pause`
- `Seek`
- `Complete`
- `Close`

For the current backend logic, `Play` is especially important because counted views only increment when the threshold is crossed on a `Play` event.

### Suggested Mapping

- on playback start or resume: send `Play`
- on manual pause: send `Pause`
- on seek/scrub: send `Seek`
- when playback reaches the end: send `Complete`
- when the page unloads or the player is torn down: send `Close`

### What To Put In `durationWatched`

`durationWatched` should be the additional watched time since the last analytics event you sent for that session.

Good example:

- viewer watched 12 more seconds since the last event
- send `durationWatched: 12`

Less helpful example:

- sending the entire lifetime watch time on every event

The backend accumulates watch time across events, so incremental values are the cleanest fit.

### What To Put In `position`

`position` should be the current playback position in seconds at the moment the event is emitted.

### Authenticated And Anonymous Viewers

The frontend does not need a different endpoint for anonymous playback.

- if the user is authenticated, send the normal bearer token
- if the user is anonymous, call the same endpoint without auth

The backend will:

- attach `UserId` when auth exists
- treat the event as anonymous when auth does not exist
- still enforce access rules

### Example Frontend Payload

```ts
const payload = {
  sessionId,
  eventType: "Play",
  eventTime: new Date().toISOString(),
  position: Math.floor(video.currentTime),
  durationWatched: watchedSinceLastEvent
};

await fetch(`${baseUrl}/api/v1/videos/${videoId}/analytics/events`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {})
  },
  body: JSON.stringify(payload)
});
```

### Practical Frontend Notes

- Do not assume fetching the manifest or segments counts as a view.
- Do not send a new `sessionId` on every event.
- Prefer incremental `durationWatched`.
- Expect some metrics to be unavailable if the backend has disabled parts of analytics collection.
- If the analytics call fails, playback should continue. Analytics should not block the player UX.

## What This System Is Good At

The current implementation is good for:

- creator dashboards
- admin dashboards
- playback behavior trends
- engagement rankings
- CSV exports
- configurable analytics collection

## Current Limits

This is not yet a full analytics platform. It does not currently provide:

- backend-derived view counts from segment traffic
- exact concurrent viewer tracking
- rich referrer or country breakdowns
- a queue or outbox for external analytics export
- pre-aggregated warehouse-style rollups

That is fine for the current product shape. The current model is meant to be practical, understandable, and easy for frontend playback clients to integrate with now.
