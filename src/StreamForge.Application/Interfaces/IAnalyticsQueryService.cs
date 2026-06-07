using StreamForge.Application.DTOs.Analytics;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Interfaces;

public interface IAnalyticsQueryService
{
    Task<long> GetSessionWatchTimeAsync(Guid videoId, Guid sessionId, CancellationToken cancellationToken = default);

    Task<AnalyticsSummaryDto> GetVideoSummaryAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<AnalyticsEngagementSummaryDto> GetVideoEngagementSummaryAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalyticsTimeSeriesPointDto>> GetVideoTimeSeriesAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<AnalyticsSummaryDto> GetOwnerSummaryAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<AnalyticsSummaryDto> GetPlatformSummaryAsync(
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalyticsTimeSeriesPointDto>> GetViewsOverTimeAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<RankedVideoAnalyticsDto>> GetRankedVideosAsync(
        Guid? ownerId,
        AnalyticsRankingType rankingType,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceBreakdownItemDto>> GetDeviceBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BrowserBreakdownItemDto>> GetBrowserBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<AuthBreakdownDto> GetAuthBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryBreakdownItemDto>> GetCategoryBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagBreakdownItemDto>> GetTagBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    Task<int> GetActiveViewerCountAsync(
        Guid? ownerId,
        DateTime since,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PeakWatchTimeItemDto>> GetPeakWatchTimeAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankedVideoAnalyticsDto>> GetReportRowsAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);
}
