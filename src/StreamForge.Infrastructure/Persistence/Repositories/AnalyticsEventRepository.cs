using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class AnalyticsEventRepository : BaseRepository<AnalyticsEvent>, IAnalyticsEventRepository
{
    public AnalyticsEventRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<AnalyticsEvent>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(evt => evt.VideoId == videoId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<AnalyticsEvent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(evt => evt.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<AnalyticsEvent>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return Guid.TryParse(sessionId, out var parsedSessionId)
            ? await _dbSet.Where(evt => evt.SessionId == parsedSessionId).ToListAsync(cancellationToken)
            : Array.Empty<AnalyticsEvent>();
    }

    public async Task<IEnumerable<AnalyticsEvent>> GetByVideoAndDateRangeAsync(
        Guid videoId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default) =>
        await _dbSet.Where(evt => evt.VideoId == videoId && evt.EventTime >= startDate && evt.EventTime <= endDate).ToListAsync(cancellationToken);

    public async Task<Dictionary<AnalyticsEventType, int>> GetEventCountsByTypeAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(evt => evt.VideoId == videoId)
            .GroupBy(evt => evt.EventType)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

    public Task<int> GetUniqueViewerCountAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        _dbSet.Where(evt => evt.VideoId == videoId && evt.UserId != null)
            .Select(evt => evt.UserId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
}
