using StreamForge.Domain.Enums;

namespace StreamForge.Application.DTOs.Analytics;

/// <summary>
/// Defines supported ranking modes for ranked analytics video lists.
/// </summary>
public enum AnalyticsRankingType
{
    /// <summary>
    /// Ranks videos by total views.
    /// </summary>
    MostWatched = 1,

    /// <summary>
    /// Ranks videos by total likes.
    /// </summary>
    MostLiked = 2,

    /// <summary>
    /// Ranks videos by total comments.
    /// </summary>
    MostCommented = 3,

    /// <summary>
    /// Ranks videos by overall engagement score.
    /// </summary>
    MostEngaged = 4
}

/// <summary>
/// Represents a client-submitted analytics event for a playback session.
/// </summary>
public sealed record RecordAnalyticsEventRequestDto(
    Guid SessionId,
    AnalyticsEventType EventType,
    DateTime? EventTime,
    int? Position,
    int? DurationWatched);

/// <summary>
/// Represents the outcome of recording a playback analytics event.
/// </summary>
public sealed record RecordAnalyticsEventResultDto(
    bool EventRecorded,
    bool ViewCountIncremented);

/// <summary>
/// Represents aggregate watch-performance metrics for a video or reporting scope.
/// </summary>
public sealed record AnalyticsSummaryDto(
    int TotalViews,
    int UniqueViewers,
    long TotalWatchTime,
    decimal AverageWatchTime,
    decimal AverageCompletionRate,
    int CompletionCount);

/// <summary>
/// Represents aggregate engagement metrics for a video or reporting scope.
/// </summary>
public sealed record AnalyticsEngagementSummaryDto(
    int LikeCount,
    int DislikeCount,
    int CommentCount,
    int EngagementScore,
    decimal? EngagementRate);

/// <summary>
/// Represents a single point in an analytics time series.
/// </summary>
public sealed record AnalyticsTimeSeriesPointDto(
    DateTime PeriodStart,
    int ViewCount);

/// <summary>
/// Represents ranked analytics metrics for a single video.
/// </summary>
public sealed record RankedVideoAnalyticsDto(
    Guid VideoId,
    string Title,
    int ViewCount,
    long TotalWatchTime,
    int LikeCount,
    int DislikeCount,
    int CommentCount,
    int EngagementScore);

/// <summary>
/// Represents viewer counts grouped by device type.
/// </summary>
public sealed record DeviceBreakdownItemDto(
    string DeviceType,
    int ViewerCount);

/// <summary>
/// Represents viewer counts grouped by browser family.
/// </summary>
public sealed record BrowserBreakdownItemDto(
    string BrowserFamily,
    int ViewerCount);

/// <summary>
/// Represents the split between authenticated and anonymous views.
/// </summary>
public sealed record AuthBreakdownDto(
    int AuthenticatedViewCount,
    int AnonymousViewCount);

/// <summary>
/// Represents view counts grouped by category.
/// </summary>
public sealed record CategoryBreakdownItemDto(
    Guid? CategoryId,
    string CategoryName,
    int ViewCount);

/// <summary>
/// Represents view counts grouped by tag.
/// </summary>
public sealed record TagBreakdownItemDto(
    Guid TagId,
    string TagName,
    int ViewCount);

/// <summary>
/// Represents a peak watch-activity bucket, typically grouped by hour.
/// </summary>
public sealed record PeakWatchTimeItemDto(
    string HourLabel,
    int WatchActivityCount);

/// <summary>
/// Represents the current active-viewer count for a reporting scope.
/// </summary>
public sealed record ActiveViewersDto(
    int ActiveViewerCount,
    int WindowMinutes);

/// <summary>
/// Represents a generated CSV export payload for analytics reporting.
/// </summary>
public sealed record CsvExportResultDto(
    string FileName,
    byte[] Content,
    string ContentType = "text/csv");
