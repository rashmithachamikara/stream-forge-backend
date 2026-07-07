using StreamForge.Application.DTOs.Analytics;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Interfaces;

/// <summary>
/// Defines read-oriented analytics queries used by reporting, dashboards, and video analytics endpoints.
/// </summary>
public interface IAnalyticsQueryService
{
    /// <summary>
    /// Gets the total recorded watch time for a specific playback session on a video.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="sessionId">Playback session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total watch time in seconds.</returns>
    Task<long> GetSessionWatchTimeAsync(Guid videoId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets summary metrics for a single video within a time window.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated analytics summary for the video.</returns>
    Task<AnalyticsSummaryDto> GetVideoSummaryAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets engagement metrics for a single video within a time window.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated engagement metrics for the video.</returns>
    Task<AnalyticsEngagementSummaryDto> GetVideoEngagementSummaryAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets time-series analytics points for a single video.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered time-series points for the video.</returns>
    Task<IReadOnlyList<AnalyticsTimeSeriesPointDto>> GetVideoTimeSeriesAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated summary metrics for all videos owned by a specific user.
    /// </summary>
    /// <param name="ownerId">Owner user identifier.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated owner-level analytics summary.</returns>
    Task<AnalyticsSummaryDto> GetOwnerSummaryAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets platform-wide summary metrics within a time window.
    /// </summary>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated platform analytics summary.</returns>
    Task<AnalyticsSummaryDto> GetPlatformSummaryAsync(
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets view-count time-series data for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered view-count time-series points.</returns>
    Task<IReadOnlyList<AnalyticsTimeSeriesPointDto>> GetViewsOverTimeAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ranked videos for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="rankingType">Ranking metric to apply.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="page">Requested page number.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged ranked video analytics results.</returns>
    Task<PagedQueryResult<RankedVideoAnalyticsDto>> GetRankedVideosAsync(
        Guid? ownerId,
        AnalyticsRankingType rankingType,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the viewer device breakdown for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Device usage breakdown items.</returns>
    Task<IReadOnlyList<DeviceBreakdownItemDto>> GetDeviceBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the viewer browser breakdown for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Browser usage breakdown items.</returns>
    Task<IReadOnlyList<BrowserBreakdownItemDto>> GetBrowserBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the authenticated-versus-anonymous audience breakdown for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audience authentication breakdown.</returns>
    Task<AuthBreakdownDto> GetAuthBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the category breakdown for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category breakdown items.</returns>
    Task<IReadOnlyList<CategoryBreakdownItemDto>> GetCategoryBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tag breakdown for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tag breakdown items.</returns>
    Task<IReadOnlyList<TagBreakdownItemDto>> GetTagBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active viewer count for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="since">Lower bound used to define an active viewer window.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active viewer count.</returns>
    Task<int> GetActiveViewerCountAsync(
        Guid? ownerId,
        DateTime since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets peak watch-time periods for either the platform or a single owner scope.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Peak watch-time result items.</returns>
    Task<IReadOnlyList<PeakWatchTimeItemDto>> GetPeakWatchTimeAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets flattened ranked rows for analytics report export.
    /// </summary>
    /// <param name="ownerId">Optional owner user identifier. <see langword="null"/> targets the full platform scope.</param>
    /// <param name="from">Inclusive range start.</param>
    /// <param name="to">Inclusive range end.</param>
    /// <param name="minimumViewWatchSeconds">Minimum watch duration required for a view to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Report-ready analytics rows.</returns>
    Task<IReadOnlyList<RankedVideoAnalyticsDto>> GetReportRowsAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default);
}
