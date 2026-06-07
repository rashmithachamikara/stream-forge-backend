using StreamForge.Domain.Enums;

namespace StreamForge.Application.DTOs.Analytics;

public enum AnalyticsRankingType
{
    MostWatched = 1,
    MostLiked = 2,
    MostCommented = 3,
    MostEngaged = 4
}

public sealed record RecordAnalyticsEventRequestDto(
    Guid SessionId,
    AnalyticsEventType EventType,
    DateTime? EventTime,
    int? Position,
    int? DurationWatched);

public sealed record RecordAnalyticsEventResultDto(
    bool EventRecorded,
    bool ViewCountIncremented);

public sealed record AnalyticsSummaryDto(
    int TotalViews,
    int UniqueViewers,
    long TotalWatchTime,
    decimal AverageWatchTime,
    decimal AverageCompletionRate,
    int CompletionCount);

public sealed record AnalyticsEngagementSummaryDto(
    int LikeCount,
    int DislikeCount,
    int CommentCount,
    int EngagementScore,
    decimal? EngagementRate);

public sealed record AnalyticsTimeSeriesPointDto(
    DateTime PeriodStart,
    int ViewCount);

public sealed record RankedVideoAnalyticsDto(
    Guid VideoId,
    string Title,
    int ViewCount,
    long TotalWatchTime,
    int LikeCount,
    int DislikeCount,
    int CommentCount,
    int EngagementScore);

public sealed record DeviceBreakdownItemDto(
    string DeviceType,
    int ViewerCount);

public sealed record BrowserBreakdownItemDto(
    string BrowserFamily,
    int ViewerCount);

public sealed record AuthBreakdownDto(
    int AuthenticatedViewCount,
    int AnonymousViewCount);

public sealed record CategoryBreakdownItemDto(
    Guid? CategoryId,
    string CategoryName,
    int ViewCount);

public sealed record TagBreakdownItemDto(
    Guid TagId,
    string TagName,
    int ViewCount);

public sealed record PeakWatchTimeItemDto(
    string HourLabel,
    int WatchActivityCount);

public sealed record ActiveViewersDto(
    int ActiveViewerCount,
    int WindowMinutes);

public sealed record CsvExportResultDto(
    string FileName,
    byte[] Content,
    string ContentType = "text/csv");
