# Phase 9 - Analytics

Status: [ ] Planned

## Purpose

Add configurable playback and engagement analytics for videos, creators, and administrators. Phase 9 should collect enough event and engagement data to power owner dashboards, admin dashboards, time-series reporting, engagement rankings, and downloadable reports without coupling controllers to aggregation logic.

## Strategy

- Reuse the existing `AnalyticsEvents` table for playback-event ingestion.
- Reuse existing denormalized counters such as `Videos.ViewCount` where they improve read performance.
- Add analytics configuration so operators can disable analytics entirely or turn off high-write/high-cost collection paths.
- Keep raw event ingestion and reporting behind Application use cases; controllers stay thin.
- Support both authenticated and anonymous viewers when enabled by configuration.
- Treat owner analytics and admin analytics as separate authorization surfaces.

## Planned Scope

- Playback analytics event ingestion for watchable videos.
- Thresholded view counting and accumulated watch-time metrics.
- Owner-facing video and portfolio analytics.
- Admin-facing platform analytics.
- Engagement analytics using existing likes/dislikes/comments data.
- CSV-style report/export endpoints.
- Configurable analytics collection, including coarse and granular toggles.

## Planned API Surface

### Playback Event Ingestion

- `POST /api/v1/videos/{videoId}/analytics/events`

### Owner Analytics

- `GET /api/v1/videos/{videoId}/analytics/summary`
- `GET /api/v1/videos/{videoId}/analytics/timeseries?from=&to=&bucket=day`
- `GET /api/v1/videos/{videoId}/analytics/engagement`
- `GET /api/v1/me/analytics/summary?from=&to=`
- `GET /api/v1/me/analytics/top-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/me/analytics/most-liked-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/me/analytics/most-commented-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/me/analytics/most-engaged-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/me/analytics/device-breakdown?from=&to=`
- `GET /api/v1/me/analytics/reports/videos?from=&to=&format=csv`

### Admin Analytics

- `GET /api/v1/admin/analytics/summary?from=&to=`
- `GET /api/v1/admin/analytics/views-over-time?from=&to=&bucket=day`
- `GET /api/v1/admin/analytics/most-watched-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/admin/analytics/most-liked-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/admin/analytics/most-commented-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/admin/analytics/most-engaged-videos?from=&to=&page=&pageSize=`
- `GET /api/v1/admin/analytics/active-viewers`
- `GET /api/v1/admin/analytics/peak-watch-time?from=&to=`
- `GET /api/v1/admin/analytics/device-breakdown?from=&to=`
- `GET /api/v1/admin/analytics/reports/overview?from=&to=&format=csv`

## Metric Definitions

### Playback Metrics

- `Total Views`: denormalized counted views using one counted view per `VideoId + SessionId` after meaningful playback.
- `Meaningful Playback`: at least one `Play` event plus accumulated `DurationWatched >= MinimumViewWatchSeconds`.
- `Views Over Time (Overall)`: platform-wide or owner-scoped daily counts for a selected date range.
- `Total Watch Time`: sum of `DurationWatched`.
- `Avg Watch Time`: `TotalWatchTime / CountedViews` when views exist.
- `Avg Completion`: average completion rate over qualifying viewing sessions.
- `Active Viewers`: distinct sessions with at least one playback event in the last `ActiveViewerWindowMinutes`.
- `Peak Watch Time`: hour-of-day watch-activity heatmap over a selected range, for example `09:00 -> 450`, `18:00 -> 520`.

### Engagement Metrics

- `Most Liked Videos`: highest like count.
- `Like vs Dislike`: raw like and dislike counts per video.
- `Most Commented Videos`: highest total comment count, including replies unless implementation explicitly chooses top-level only.
- `Most Engaged Videos`: explicit weighted score such as `likes + comments + dislikes`, with the formula documented in code and API docs.
- `Engagement Rate`: engagement actions divided by counted views where views exist.

## Analytics Configuration

Add an `Analytics` options section following the existing application options pattern.

### Coarse Toggles

- `Enabled`
- `IngestionEnabled`
- `ReportingEnabled`
- `AdminReportingEnabled`

### High-Write / High-Cost Collection Toggles

- `CollectRawEvents`
- `CollectAnonymousEvents`
- `CollectUserAgent`
- `CollectIpAddress`
- `CollectPauseEvents`
- `CollectSeekEvents`
- `CollectCloseEvents`
- `EnableDeviceBreakdown`
- `EnableActiveViewerMetrics`
- `EnablePeakWatchTimeMetrics`

### Behavior Settings

- `MinimumViewWatchSeconds`
- `ActiveViewerWindowMinutes`

### Config Semantics

- When `Enabled=false`, analytics ingestion and analytics reporting are disabled.
- When `IngestionEnabled=false`, playback-event ingestion is disabled while existing aggregate reads may remain available.
- When `ReportingEnabled=false`, owner analytics/report endpoints are disabled.
- When `AdminReportingEnabled=false`, admin analytics/report endpoints are disabled.
- When a granular collection toggle is off, the related source data must not be collected or processed.
- Metrics that depend on disabled collection should return a clear unavailable/disabled result rather than misleading zeros.

## Playback Event Model

### Request Shape

- `sessionId`
- `eventType`
- `eventTime`
- `position`
- `durationWatched`

### Supported Event Types

- `Play`
- `Pause`
- `Seek`
- `Complete`
- `Close`

### Ingestion Rules

- Accept events only for accessible videos.
- Enrich with current user ID when authenticated.
- Enrich with IP and user agent only when configuration allows it.
- Ignore disabled event categories cleanly.
- Keep ingestion synchronous in Phase 9; do not introduce queue-based analytics processing yet.

## Authorization

- Playback analytics ingestion may be anonymous if the video is viewable and anonymous collection is enabled.
- Owner analytics endpoints require ownership or existing video-manage permission.
- Admin analytics endpoints require admin role.
- Raw IP addresses and raw user agents must not be exposed in responses.

## Repository And Application Work

- Extend `IAnalyticsEventRepository` with aggregation helpers for:
  - thresholded session qualification
  - total watch time
  - completion counts/rates
  - unique viewers
  - top watched videos
  - daily time-series
  - hour-of-day heatmap
  - active session count
  - device-class grouped counts
  - owner-scoped and global rollups
- Add Application DTOs/use cases for:
  - playback analytics ingestion
  - video analytics summary
  - owner summary and rankings
  - admin summary and rankings
  - device breakdown
  - active-viewer metrics
  - peak-watch-time heatmap
  - engagement analytics summary
  - CSV exports
- Keep configuration gating inside Application use cases rather than in controllers.

## Reporting

- Support CSV download responses in Phase 9.
- Reports are request/response based only in this phase.
- Do not add scheduled jobs, email delivery, or offline export generation yet.

## Dependencies

- Phase 4 authentication/current-user abstractions.
- Phase 4 video authorization rules.
- Phase 7.5 content and management APIs.
- Phase 8 engagement data for likes/dislikes/comments and rankings.
- Existing `AnalyticsEvents` schema and `Videos.ViewCount`.

## Acceptance Criteria

- Playback events can be ingested for accessible videos when analytics ingestion is enabled.
- Counted views increment only after thresholded meaningful playback and only once per video/session.
- Owner analytics expose summary, trends, top videos, device breakdown, and engagement rankings for manageable videos.
- Admin analytics expose platform-wide summary, views over time, most watched, most liked, most commented, most engaged, active viewers, peak watch time, and device breakdown.
- CSV report endpoints work for owner and admin scopes.
- Analytics configuration can disable the entire feature or selectively disable high-write collection paths.
- Disabled metrics surface as unavailable/disabled rather than incorrect zeros.

## Test Plan

- Test authenticated and anonymous event ingestion with configuration on/off combinations.
- Test thresholded view counting, duplicate session suppression, and watch-time accumulation.
- Test owner/admin authorization across analytics endpoints.
- Test most-watched, most-liked, most-commented, and most-engaged ranking outputs.
- Test active-viewer calculation using the configured recent-event window.
- Test peak-watch-time hour buckets across selected date ranges.
- Test device breakdown only when user-agent collection and device analytics are enabled.
- Test CSV exports for scope, filtering, and authorization.
- Build successfully after implementation.

## Notes

- Device breakdown should use coarse user-agent classification such as desktop, mobile, tablet, and unknown/bot.
- `Peak Watch Time` in this phase means hour-of-day watch-activity heatmap, not peak concurrent viewers.
- If implementation finds gaps in the existing analytics schema, document and add only the minimum required schema changes.
