using Microsoft.EntityFrameworkCore;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence;

public sealed class AnalyticsQueryService : IAnalyticsQueryService
{
    private readonly StreamForgeDbContext _dbContext;

    public AnalyticsQueryService(StreamForgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<long> GetSessionWatchTimeAsync(Guid videoId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AnalyticsEvents
            .AsNoTracking()
            .Where(evt => evt.VideoId == videoId && evt.SessionId == sessionId)
            .SumAsync(evt => (long?)(evt.DurationWatched ?? 0), cancellationToken) ?? 0L;
    }

    public async Task<AnalyticsSummaryDto> GetVideoSummaryAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var metrics = await GetVideoMetricsAsync(new[] { videoId }, from, to, minimumViewWatchSeconds, cancellationToken);
        var metric = metrics.GetValueOrDefault(videoId) ?? VideoMetric.Empty;
        return ToSummary(metric);
    }

    public async Task<AnalyticsEngagementSummaryDto> GetVideoEngagementSummaryAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var metrics = await GetVideoMetricsAsync(new[] { videoId }, from, to, minimumViewWatchSeconds, cancellationToken);
        var metric = metrics.GetValueOrDefault(videoId) ?? VideoMetric.Empty;
        return ToEngagementSummary(metric);
    }

    public async Task<IReadOnlyList<AnalyticsTimeSeriesPointDto>> GetVideoTimeSeriesAsync(
        Guid videoId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var qualifiedViews = await GetQualifiedViewsQuery(from, to, minimumViewWatchSeconds)
            .Where(view => view.VideoId == videoId)
            .Select(view => view.FirstEventTime.Date)
            .ToListAsync(cancellationToken);

        return BuildDateSeries(from, to, qualifiedViews);
    }

    public async Task<AnalyticsSummaryDto> GetOwnerSummaryAsync(
        Guid ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var videoIds = await GetScopedVideoIdsAsync(ownerId, cancellationToken);
        return ToSummary(await GetAggregatedMetricAsync(videoIds, from, to, minimumViewWatchSeconds, cancellationToken));
    }

    public async Task<AnalyticsSummaryDto> GetPlatformSummaryAsync(
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var videoIds = await GetScopedVideoIdsAsync(null, cancellationToken);
        return ToSummary(await GetAggregatedMetricAsync(videoIds, from, to, minimumViewWatchSeconds, cancellationToken));
    }

    public async Task<IReadOnlyList<AnalyticsTimeSeriesPointDto>> GetViewsOverTimeAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var qualifiedViews = GetQualifiedViewsQuery(from, to, minimumViewWatchSeconds);
        if (ownerId.HasValue)
        {
            qualifiedViews = qualifiedViews.Where(view => view.OwnerId == ownerId.Value);
        }

        var dates = await qualifiedViews
            .Select(view => view.FirstEventTime.Date)
            .ToListAsync(cancellationToken);

        return BuildDateSeries(from, to, dates);
    }

    public async Task<PagedQueryResult<RankedVideoAnalyticsDto>> GetRankedVideosAsync(
        Guid? ownerId,
        AnalyticsRankingType rankingType,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var videos = await GetScopedVideosAsync(ownerId, cancellationToken);
        var metrics = await GetVideoMetricsAsync(videos.Select(video => video.Id).ToArray(), from, to, minimumViewWatchSeconds, cancellationToken);

        var items = videos
            .Select(video =>
            {
                var metric = metrics.GetValueOrDefault(video.Id) ?? VideoMetric.Empty;
                return new RankedVideoAnalyticsDto(
                    video.Id,
                    video.Title,
                    metric.ViewCount,
                    metric.TotalWatchTime,
                    metric.LikeCount,
                    metric.DislikeCount,
                    metric.CommentCount,
                    metric.EngagementScore);
            });

        items = rankingType switch
        {
            AnalyticsRankingType.MostLiked => items.OrderByDescending(item => item.LikeCount).ThenByDescending(item => item.ViewCount).ThenBy(item => item.Title),
            AnalyticsRankingType.MostCommented => items.OrderByDescending(item => item.CommentCount).ThenByDescending(item => item.ViewCount).ThenBy(item => item.Title),
            AnalyticsRankingType.MostEngaged => items.OrderByDescending(item => item.EngagementScore).ThenByDescending(item => item.ViewCount).ThenBy(item => item.Title),
            _ => items.OrderByDescending(item => item.ViewCount).ThenByDescending(item => item.TotalWatchTime).ThenBy(item => item.Title)
        };

        var totalCount = items.Count();
        var pagedItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PagedQueryResult<RankedVideoAnalyticsDto>(pagedItems, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<DeviceBreakdownItemDto>> GetDeviceBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AnalyticsEvents
            .AsNoTracking()
            .Where(evt => evt.EventTime >= from && evt.EventTime <= to);

        if (ownerId.HasValue)
        {
            query = query.Where(evt => evt.Video.UploaderId == ownerId.Value);
        }

        var rows = await query
            .Where(evt => evt.UserAgent != null)
            .Select(evt => new { evt.SessionId, evt.UserAgent })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => ClassifyDevice(row.UserAgent))
            .Select(group => new DeviceBreakdownItemDto(group.Key, group.Count()))
            .OrderByDescending(item => item.ViewerCount)
            .ThenBy(item => item.DeviceType)
            .ToArray();
    }

    public async Task<IReadOnlyList<BrowserBreakdownItemDto>> GetBrowserBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var qualifiedViews = await GetQualifiedViewsQuery(from, to, minimumViewWatchSeconds)
            .Where(view => !ownerId.HasValue || view.OwnerId == ownerId.Value)
            .Select(view => new { view.SessionId, view.UserAgent })
            .ToListAsync(cancellationToken);

        return qualifiedViews
            .GroupBy(view => ClassifyBrowser(view.UserAgent))
            .Select(group => new BrowserBreakdownItemDto(group.Key, group.Select(item => item.SessionId).Distinct().Count()))
            .OrderByDescending(item => item.ViewerCount)
            .ThenBy(item => item.BrowserFamily)
            .ToArray();
    }

    public async Task<AuthBreakdownDto> GetAuthBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var qualifiedViews = await GetQualifiedViewsQuery(from, to, minimumViewWatchSeconds)
            .Where(view => !ownerId.HasValue || view.OwnerId == ownerId.Value)
            .Select(view => view.HasAuthenticatedUser)
            .ToListAsync(cancellationToken);

        return new AuthBreakdownDto(
            AuthenticatedViewCount: qualifiedViews.Count(isAuthenticated => isAuthenticated),
            AnonymousViewCount: qualifiedViews.Count(isAuthenticated => !isAuthenticated));
    }

    public async Task<IReadOnlyList<CategoryBreakdownItemDto>> GetCategoryBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var qualifiedViews = await GetQualifiedViewsQuery(from, to, minimumViewWatchSeconds)
            .Where(view => !ownerId.HasValue || view.OwnerId == ownerId.Value)
            .Select(view => new { view.CategoryId, view.CategoryName })
            .ToListAsync(cancellationToken);

        return qualifiedViews
            .GroupBy(view => new { view.CategoryId, CategoryName = view.CategoryName ?? "Uncategorized" })
            .Select(group => new CategoryBreakdownItemDto(group.Key.CategoryId, group.Key.CategoryName, group.Count()))
            .OrderByDescending(item => item.ViewCount)
            .ThenBy(item => item.CategoryName)
            .ToArray();
    }

    public async Task<IReadOnlyList<TagBreakdownItemDto>> GetTagBreakdownAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var qualifiedVideoViews = await GetQualifiedViewsQuery(from, to, minimumViewWatchSeconds)
            .Where(view => !ownerId.HasValue || view.OwnerId == ownerId.Value)
            .Select(view => view.VideoId)
            .ToListAsync(cancellationToken);

        if (qualifiedVideoViews.Count == 0)
        {
            return Array.Empty<TagBreakdownItemDto>();
        }

        var videoViewCounts = qualifiedVideoViews
            .GroupBy(videoId => videoId)
            .ToDictionary(group => group.Key, group => group.Count());

        var tags = await _dbContext.VideoTags
            .AsNoTracking()
            .Where(videoTag => videoViewCounts.Keys.Contains(videoTag.VideoId))
            .Select(videoTag => new { videoTag.VideoId, videoTag.TagId, videoTag.Tag.Name })
            .ToListAsync(cancellationToken);

        return tags
            .GroupBy(item => new { item.TagId, item.Name })
            .Select(group => new TagBreakdownItemDto(
                group.Key.TagId,
                group.Key.Name,
                group.Sum(item => videoViewCounts.GetValueOrDefault(item.VideoId))))
            .OrderByDescending(item => item.ViewCount)
            .ThenBy(item => item.TagName)
            .ToArray();
    }

    public Task<int> GetActiveViewerCountAsync(Guid? ownerId, DateTime since, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AnalyticsEvents
            .AsNoTracking()
            .Where(evt => evt.EventTime >= since);

        if (ownerId.HasValue)
        {
            query = query.Where(evt => evt.Video.UploaderId == ownerId.Value);
        }

        return query
            .Select(evt => evt.SessionId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PeakWatchTimeItemDto>> GetPeakWatchTimeAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AnalyticsEvents
            .AsNoTracking()
            .Where(evt => evt.EventTime >= from && evt.EventTime <= to && (evt.DurationWatched ?? 0) > 0);

        if (ownerId.HasValue)
        {
            query = query.Where(evt => evt.Video.UploaderId == ownerId.Value);
        }

        var grouped = await query
            .GroupBy(evt => evt.EventTime.Hour)
            .Select(group => new { Hour = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, 24)
            .Select(hour => new PeakWatchTimeItemDto($"{hour:00}:00", grouped.FirstOrDefault(item => item.Hour == hour)?.Count ?? 0))
            .ToArray();
    }

    public async Task<IReadOnlyList<RankedVideoAnalyticsDto>> GetReportRowsAsync(
        Guid? ownerId,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken = default)
    {
        var videos = await GetScopedVideosAsync(ownerId, cancellationToken);
        var metrics = await GetVideoMetricsAsync(videos.Select(video => video.Id).ToArray(), from, to, minimumViewWatchSeconds, cancellationToken);

        return videos
            .Select(video =>
            {
                var metric = metrics.GetValueOrDefault(video.Id) ?? VideoMetric.Empty;
                return new RankedVideoAnalyticsDto(
                    video.Id,
                    video.Title,
                    metric.ViewCount,
                    metric.TotalWatchTime,
                    metric.LikeCount,
                    metric.DislikeCount,
                    metric.CommentCount,
                    metric.EngagementScore);
            })
            .OrderByDescending(item => item.ViewCount)
            .ThenByDescending(item => item.EngagementScore)
            .ThenBy(item => item.Title)
            .ToArray();
    }

    private async Task<Dictionary<Guid, VideoMetric>> GetVideoMetricsAsync(
        IReadOnlyCollection<Guid> videoIds,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken)
    {
        if (videoIds.Count == 0)
        {
            return new Dictionary<Guid, VideoMetric>();
        }

        var qualifiedViews = await GetQualifiedViewsQuery(from, to, minimumViewWatchSeconds)
            .Where(view => videoIds.Contains(view.VideoId))
            .Select(view => new
            {
                view.VideoId,
                view.SessionId,
                view.TotalDuration,
                view.HasComplete
            })
            .ToListAsync(cancellationToken);

        var likeCounts = await _dbContext.VideoReactions
            .AsNoTracking()
            .Where(reaction => videoIds.Contains(reaction.VideoId) && reaction.CreatedAt >= from && reaction.CreatedAt <= to)
            .GroupBy(reaction => new { reaction.VideoId, reaction.ReactionType })
            .Select(group => new { group.Key.VideoId, group.Key.ReactionType, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var commentCounts = await _dbContext.VideoComments
            .AsNoTracking()
            .Where(comment => videoIds.Contains(comment.VideoId) && comment.CreatedAt >= from && comment.CreatedAt <= to)
            .GroupBy(comment => comment.VideoId)
            .Select(group => new { VideoId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var metrics = videoIds.ToDictionary(videoId => videoId, _ => VideoMetric.Empty);

        foreach (var viewGroup in qualifiedViews.GroupBy(view => view.VideoId))
        {
            var totalViews = viewGroup.Count();
            var totalWatchTime = viewGroup.Sum(item => item.TotalDuration);
            var completionCount = viewGroup.Count(item => item.HasComplete);
            metrics[viewGroup.Key] = metrics[viewGroup.Key] with
            {
                ViewCount = totalViews,
                UniqueViewers = viewGroup.Select(item => item.SessionId).Distinct().Count(),
                TotalWatchTime = totalWatchTime,
                CompletionCount = completionCount
            };
        }

        foreach (var item in likeCounts)
        {
            var metric = metrics[item.VideoId];
            metrics[item.VideoId] = item.ReactionType == ReactionType.Like
                ? metric with { LikeCount = item.Count }
                : metric with { DislikeCount = item.Count };
        }

        foreach (var item in commentCounts)
        {
            metrics[item.VideoId] = metrics[item.VideoId] with { CommentCount = item.Count };
        }

        return metrics.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                var metric = pair.Value;
                return metric with
                {
                    EngagementScore = metric.LikeCount + metric.DislikeCount + metric.CommentCount
                };
            });
    }

    private async Task<VideoMetric> GetAggregatedMetricAsync(
        IReadOnlyCollection<Guid> videoIds,
        DateTime from,
        DateTime to,
        int minimumViewWatchSeconds,
        CancellationToken cancellationToken)
    {
        var metrics = await GetVideoMetricsAsync(videoIds, from, to, minimumViewWatchSeconds, cancellationToken);
        return metrics.Values.Aggregate(VideoMetric.Empty, (total, metric) => total with
        {
            ViewCount = total.ViewCount + metric.ViewCount,
            UniqueViewers = total.UniqueViewers + metric.UniqueViewers,
            TotalWatchTime = total.TotalWatchTime + metric.TotalWatchTime,
            CompletionCount = total.CompletionCount + metric.CompletionCount,
            LikeCount = total.LikeCount + metric.LikeCount,
            DislikeCount = total.DislikeCount + metric.DislikeCount,
            CommentCount = total.CommentCount + metric.CommentCount,
            EngagementScore = total.EngagementScore + metric.EngagementScore
        });
    }

    private IQueryable<QualifiedViewProjection> GetQualifiedViewsQuery(DateTime from, DateTime to, int minimumViewWatchSeconds)
    {
        return _dbContext.AnalyticsEvents
            .AsNoTracking()
            .Where(evt => evt.EventTime >= from && evt.EventTime <= to)
            .GroupBy(evt => new { evt.VideoId, evt.SessionId, evt.Video.UploaderId })
            .Select(group => new QualifiedViewProjection
            {
                VideoId = group.Key.VideoId,
                SessionId = group.Key.SessionId,
                OwnerId = group.Key.UploaderId,
                CategoryId = group.Select(evt => evt.Video.CategoryId).FirstOrDefault(),
                CategoryName = group.Select(evt => evt.Video.Category != null ? evt.Video.Category.Name : null).FirstOrDefault(),
                FirstEventTime = group.Min(evt => evt.EventTime),
                TotalDuration = group.Sum(evt => evt.DurationWatched ?? 0),
                HasPlay = group.Any(evt => evt.EventType == AnalyticsEventType.Play),
                HasComplete = group.Any(evt => evt.EventType == AnalyticsEventType.Complete),
                HasAuthenticatedUser = group.Any(evt => evt.UserId != null),
                UserAgent = group.Select(evt => evt.UserAgent).FirstOrDefault()
            })
            .Where(view => view.HasPlay && view.TotalDuration >= minimumViewWatchSeconds);
    }

    private async Task<IReadOnlyList<Guid>> GetScopedVideoIdsAsync(Guid? ownerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Videos
            .AsNoTracking()
            .Where(video => !ownerId.HasValue || video.UploaderId == ownerId.Value)
            .Select(video => video.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Video>> GetScopedVideosAsync(Guid? ownerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Videos
            .AsNoTracking()
            .Where(video => !ownerId.HasValue || video.UploaderId == ownerId.Value)
            .OrderBy(video => video.Title)
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyList<AnalyticsTimeSeriesPointDto> BuildDateSeries(DateTime from, DateTime to, IReadOnlyCollection<DateTime> dates)
    {
        var lookup = dates
            .GroupBy(date => date.Date)
            .ToDictionary(group => group.Key, group => group.Count());

        var start = from.Date;
        var end = to.Date;
        var days = (end - start).Days + 1;

        return Enumerable.Range(0, Math.Max(days, 0))
            .Select(offset =>
            {
                var date = start.AddDays(offset);
                return new AnalyticsTimeSeriesPointDto(date, lookup.GetValueOrDefault(date));
            })
            .ToArray();
    }

    private static AnalyticsSummaryDto ToSummary(VideoMetric metric)
    {
        var averageWatchTime = metric.ViewCount == 0 ? 0m : decimal.Round((decimal)metric.TotalWatchTime / metric.ViewCount, 2);
        var averageCompletionRate = metric.ViewCount == 0 ? 0m : decimal.Round((decimal)metric.CompletionCount * 100m / metric.ViewCount, 2);
        return new AnalyticsSummaryDto(
            metric.ViewCount,
            metric.UniqueViewers,
            metric.TotalWatchTime,
            averageWatchTime,
            averageCompletionRate,
            metric.CompletionCount);
    }

    private static AnalyticsEngagementSummaryDto ToEngagementSummary(VideoMetric metric)
    {
        decimal? engagementRate = metric.ViewCount == 0
            ? null
            : decimal.Round((decimal)metric.EngagementScore * 100m / metric.ViewCount, 2);

        return new AnalyticsEngagementSummaryDto(
            metric.LikeCount,
            metric.DislikeCount,
            metric.CommentCount,
            metric.EngagementScore,
            engagementRate);
    }

    private static string ClassifyDevice(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }

        var normalized = userAgent.ToLowerInvariant();
        if (normalized.Contains("bot") || normalized.Contains("spider") || normalized.Contains("crawl"))
        {
            return "bot";
        }

        if (normalized.Contains("ipad") || normalized.Contains("tablet"))
        {
            return "tablet";
        }

        if (normalized.Contains("mobile") || normalized.Contains("android") || normalized.Contains("iphone"))
        {
            return "mobile";
        }

        return "desktop";
    }

    private static string ClassifyBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }

        var normalized = userAgent.ToLowerInvariant();
        if (normalized.Contains("edg/") || normalized.Contains("edge/"))
        {
            return "edge";
        }

        if (normalized.Contains("opr/") || normalized.Contains("opera"))
        {
            return "opera";
        }

        if (normalized.Contains("chrome/") && !normalized.Contains("edg/") && !normalized.Contains("opr/"))
        {
            return "chrome";
        }

        if (normalized.Contains("firefox/"))
        {
            return "firefox";
        }

        if ((normalized.Contains("safari/") && normalized.Contains("version/")) || normalized.Contains("mobile/"))
        {
            return "safari";
        }

        return "other";
    }

    private sealed record QualifiedViewProjection
    {
        public Guid VideoId { get; init; }

        public Guid SessionId { get; init; }

        public Guid OwnerId { get; init; }

        public Guid? CategoryId { get; init; }

        public string? CategoryName { get; init; }

        public DateTime FirstEventTime { get; init; }

        public int TotalDuration { get; init; }

        public bool HasPlay { get; init; }

        public bool HasComplete { get; init; }

        public bool HasAuthenticatedUser { get; init; }

        public string? UserAgent { get; init; }
    }

    private sealed record VideoMetric
    {
        public static VideoMetric Empty => new();

        public int ViewCount { get; init; }

        public int UniqueViewers { get; init; }

        public long TotalWatchTime { get; init; }

        public int CompletionCount { get; init; }

        public int LikeCount { get; init; }

        public int DislikeCount { get; init; }

        public int CommentCount { get; init; }

        public int EngagementScore { get; init; }
    }
}
