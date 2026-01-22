using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for AnalyticsEvent entity
/// </summary>
public interface IAnalyticsEventRepository : IRepository<AnalyticsEvent>
{
    /// <summary>
    /// Gets analytics events by video ID
    /// </summary>
    Task<IEnumerable<AnalyticsEvent>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics events by user ID
    /// </summary>
    Task<IEnumerable<AnalyticsEvent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics events by session ID
    /// </summary>
    Task<IEnumerable<AnalyticsEvent>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics events by video and date range
    /// </summary>
    Task<IEnumerable<AnalyticsEvent>> GetByVideoAndDateRangeAsync(
        Guid videoId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets event count by type for a video
    /// </summary>
    Task<Dictionary<AnalyticsEventType, int>> GetEventCountsByTypeAsync(
        Guid videoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unique viewer count for a video
    /// </summary>
    Task<int> GetUniqueViewerCountAsync(Guid videoId, CancellationToken cancellationToken = default);
}
