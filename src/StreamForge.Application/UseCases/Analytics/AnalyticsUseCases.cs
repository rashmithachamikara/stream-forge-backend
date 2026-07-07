using System.Text;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Analytics;

public sealed class RecordAnalyticsEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly IRequestMetadataAccessor _requestMetadataAccessor;
    private readonly AnalyticsOptions _options;

    public RecordAnalyticsEventService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IAnalyticsQueryService analyticsQueryService,
        IRequestMetadataAccessor requestMetadataAccessor,
        AnalyticsOptions options)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _analyticsQueryService = analyticsQueryService;
        _requestMetadataAccessor = requestMetadataAccessor;
        _options = options;
    }

    public async Task<RecordAnalyticsEventResultDto> Handle(Guid videoId, RecordAnalyticsEventRequestDto request, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: true, requireAdminReporting: false, requireReporting: false);
        AnalyticsGuards.EnsureEventCollectionEnabled(_options, request.EventType);

        if (!_currentUserService.IsAuthenticated && !_options.CollectAnonymousEvents)
        {
            return new RecordAnalyticsEventResultDto(EventRecorded: false, ViewCountIncremented: false);
        }

        var canView = await _authorizationService.CanViewVideoAsync(videoId, _currentUserService.UserId, _currentUserService.Role, cancellationToken: cancellationToken);
        if (!canView)
        {
            throw new System.UnauthorizedAccessException("You do not have access to this video");
        }

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        if (video.Status != VideoStatus.Ready)
        {
            throw new InvalidOperationException("Video is not ready");
        }

        if (!_options.CollectRawEvents)
        {
            return new RecordAnalyticsEventResultDto(EventRecorded: false, ViewCountIncremented: false);
        }

        var priorWatchTime = await _analyticsQueryService.GetSessionWatchTimeAsync(videoId, request.SessionId, cancellationToken);
        var additionalWatchTime = Math.Max(0, request.DurationWatched ?? 0);
        var shouldIncrementView =
            request.EventType == AnalyticsEventType.Play &&
            additionalWatchTime > 0 &&
            priorWatchTime < _options.MinimumViewWatchSeconds &&
            priorWatchTime + additionalWatchTime >= _options.MinimumViewWatchSeconds;

        var ipAddress = _options.CollectIpAddress ? (_requestMetadataAccessor.IpAddress ?? "unknown") : "disabled";
        var userAgent = _options.CollectUserAgent ? _requestMetadataAccessor.UserAgent : null;

        await _unitOfWork.AnalyticsEvents.AddAsync(
            AnalyticsEvent.Create(
                videoId,
                request.SessionId,
                request.EventType,
                ipAddress,
                _currentUserService.UserId,
                request.EventTime,
                request.Position,
                request.DurationWatched,
                userAgent),
            cancellationToken);

        if (shouldIncrementView)
        {
            video.IncrementViewCount();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new RecordAnalyticsEventResultDto(EventRecorded: true, ViewCountIncremented: shouldIncrementView);
    }
}

public sealed class GetVideoAnalyticsSummaryService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly AnalyticsOptions _options;

    public GetVideoAnalyticsSummaryService(IAnalyticsQueryService analyticsQueryService, ICurrentUserService currentUserService, IAuthorizationService authorizationService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _options = options;
    }

    public async Task<AnalyticsSummaryDto> Handle(Guid videoId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        await AnalyticsGuards.EnsureCanManageVideoAsync(videoId, _currentUserService, _authorizationService, cancellationToken);
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetVideoSummaryAsync(videoId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetVideoAnalyticsEngagementService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly AnalyticsOptions _options;

    public GetVideoAnalyticsEngagementService(IAnalyticsQueryService analyticsQueryService, ICurrentUserService currentUserService, IAuthorizationService authorizationService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _options = options;
    }

    public async Task<AnalyticsEngagementSummaryDto> Handle(Guid videoId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        await AnalyticsGuards.EnsureCanManageVideoAsync(videoId, _currentUserService, _authorizationService, cancellationToken);
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetVideoEngagementSummaryAsync(videoId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetVideoAnalyticsTimeSeriesService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly AnalyticsOptions _options;

    public GetVideoAnalyticsTimeSeriesService(IAnalyticsQueryService analyticsQueryService, ICurrentUserService currentUserService, IAuthorizationService authorizationService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _options = options;
    }

    public async Task<IReadOnlyList<AnalyticsTimeSeriesPointDto>> Handle(Guid videoId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        await AnalyticsGuards.EnsureCanManageVideoAsync(videoId, _currentUserService, _authorizationService, cancellationToken);
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetVideoTimeSeriesAsync(videoId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetMyAnalyticsSummaryService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly AnalyticsOptions _options;

    public GetMyAnalyticsSummaryService(IAnalyticsQueryService analyticsQueryService, ICurrentUserService currentUserService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _currentUserService = currentUserService;
        _options = options;
    }

    public async Task<AnalyticsSummaryDto> Handle(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        var ownerId = _currentUserService.UserId ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetOwnerSummaryAsync(ownerId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetRankedVideosAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetRankedVideosAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<PagedResponseDto<RankedVideoAnalyticsDto>> Handle(Guid? ownerId, AnalyticsRankingType rankingType, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        var range = AnalyticsRanges.Normalize(from, to);
        var normalizedPage = AnalyticsRanges.NormalizePage(page);
        var normalizedPageSize = AnalyticsRanges.NormalizePageSize(pageSize);
        var result = await _analyticsQueryService.GetRankedVideosAsync(ownerId, rankingType, range.From, range.To, _options.MinimumViewWatchSeconds, normalizedPage, normalizedPageSize, cancellationToken);
        return AnalyticsRanges.ToPagedResponse(result);
    }
}

public sealed class GetDeviceBreakdownAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetDeviceBreakdownAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<IReadOnlyList<DeviceBreakdownItemDto>> Handle(Guid? ownerId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        if (!_options.EnableDeviceBreakdown)
        {
            throw new BusinessRuleViolationException("Device breakdown analytics are disabled");
        }

        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetDeviceBreakdownAsync(ownerId, range.From, range.To, cancellationToken);
    }
}

public sealed class GetBrowserBreakdownAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetBrowserBreakdownAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<IReadOnlyList<BrowserBreakdownItemDto>> Handle(Guid? ownerId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        if (!_options.EnableBrowserBreakdown)
        {
            throw new BusinessRuleViolationException("Browser breakdown analytics are disabled");
        }

        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetBrowserBreakdownAsync(ownerId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetAuthBreakdownAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetAuthBreakdownAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<AuthBreakdownDto> Handle(Guid? ownerId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetAuthBreakdownAsync(ownerId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetCategoryBreakdownAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetCategoryBreakdownAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<IReadOnlyList<CategoryBreakdownItemDto>> Handle(Guid? ownerId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetCategoryBreakdownAsync(ownerId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetTagBreakdownAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetTagBreakdownAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<IReadOnlyList<TagBreakdownItemDto>> Handle(Guid? ownerId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetTagBreakdownAsync(ownerId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetViewsOverTimeAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetViewsOverTimeAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<IReadOnlyList<AnalyticsTimeSeriesPointDto>> Handle(Guid? ownerId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetViewsOverTimeAsync(ownerId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

public sealed class GetActiveViewersAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetActiveViewersAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<ActiveViewersDto> Handle(Guid? ownerId, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        if (!_options.EnableActiveViewerMetrics)
        {
            throw new BusinessRuleViolationException("Active viewer analytics are disabled");
        }

        var count = await _analyticsQueryService.GetActiveViewerCountAsync(ownerId, DateTime.UtcNow.AddMinutes(-_options.ActiveViewerWindowMinutes), cancellationToken);
        return new ActiveViewersDto(count, _options.ActiveViewerWindowMinutes);
    }
}

public sealed class GetPeakWatchTimeAnalyticsService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetPeakWatchTimeAnalyticsService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<IReadOnlyList<PeakWatchTimeItemDto>> Handle(Guid? ownerId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        if (!_options.EnablePeakWatchTimeMetrics)
        {
            throw new BusinessRuleViolationException("Peak watch time analytics are disabled");
        }

        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetPeakWatchTimeAsync(ownerId, range.From, range.To, cancellationToken);
    }
}

public sealed class ExportAnalyticsReportService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public ExportAnalyticsReportService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<CsvExportResultDto> Handle(Guid? ownerId, string filePrefix, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: false, requireReporting: true);
        var range = AnalyticsRanges.Normalize(from, to);
        var rows = await _analyticsQueryService.GetReportRowsAsync(ownerId, range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("VideoId,Title,Views,TotalWatchTime,Likes,Dislikes,Comments,EngagementScore");
        foreach (var row in rows)
        {
            builder.AppendLine($"{row.VideoId},\"{EscapeCsv(row.Title)}\",{row.ViewCount},{row.TotalWatchTime},{row.LikeCount},{row.DislikeCount},{row.CommentCount},{row.EngagementScore}");
        }

        return new CsvExportResultDto($"{filePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv", Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private static string EscapeCsv(string value) => value.Replace("\"", "\"\"");
}

public sealed class GetAdminAnalyticsSummaryService
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly AnalyticsOptions _options;

    public GetAdminAnalyticsSummaryService(IAnalyticsQueryService analyticsQueryService, AnalyticsOptions options)
    {
        _analyticsQueryService = analyticsQueryService;
        _options = options;
    }

    public async Task<AnalyticsSummaryDto> Handle(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        AnalyticsGuards.EnsureAnalyticsEnabled(_options, requireIngestion: false, requireAdminReporting: true, requireReporting: false);
        var range = AnalyticsRanges.Normalize(from, to);
        return await _analyticsQueryService.GetPlatformSummaryAsync(range.From, range.To, _options.MinimumViewWatchSeconds, cancellationToken);
    }
}

internal static class AnalyticsGuards
{
    public static void EnsureAnalyticsEnabled(AnalyticsOptions options, bool requireIngestion, bool requireAdminReporting, bool requireReporting)
    {
        if (!options.Enabled)
        {
            throw new BusinessRuleViolationException("Analytics are disabled");
        }

        if (requireIngestion && !options.IngestionEnabled)
        {
            throw new BusinessRuleViolationException("Analytics ingestion is disabled");
        }

        if (requireReporting && !options.ReportingEnabled)
        {
            throw new BusinessRuleViolationException("Analytics reporting is disabled");
        }

        if (requireAdminReporting && !options.AdminReportingEnabled)
        {
            throw new BusinessRuleViolationException("Admin analytics reporting is disabled");
        }
    }

    public static void EnsureEventCollectionEnabled(AnalyticsOptions options, AnalyticsEventType eventType)
    {
        if (eventType == AnalyticsEventType.Pause && !options.CollectPauseEvents)
        {
            throw new BusinessRuleViolationException("Pause event analytics are disabled");
        }

        if (eventType == AnalyticsEventType.Seek && !options.CollectSeekEvents)
        {
            throw new BusinessRuleViolationException("Seek event analytics are disabled");
        }

        if (eventType == AnalyticsEventType.Close && !options.CollectCloseEvents)
        {
            throw new BusinessRuleViolationException("Close event analytics are disabled");
        }
    }

    public static async Task EnsureCanManageVideoAsync(Guid videoId, ICurrentUserService currentUserService, IAuthorizationService authorizationService, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (!await authorizationService.CanManageVideoAsync(videoId, userId, currentUserService.Role, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have permission to manage this video");
        }
    }
}

internal static class AnalyticsRanges
{
    private const int _defaultDays = 30;
    private const int _defaultPage = 1;
    private const int _defaultPageSize = 24;
    private const int _maxPageSize = 100;

    public static (DateTime From, DateTime To) Normalize(DateTime? from, DateTime? to)
    {
        var normalizedTo = (to ?? DateTime.UtcNow).ToUniversalTime();
        var normalizedFrom = (from ?? normalizedTo.AddDays(-_defaultDays)).ToUniversalTime();
        if (normalizedFrom > normalizedTo)
        {
            throw new ArgumentException("The start date must be earlier than the end date");
        }

        return (normalizedFrom, normalizedTo);
    }

    public static int NormalizePage(int page) => page <= 0 ? _defaultPage : page;

    public static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return _defaultPageSize;
        }

        return Math.Min(pageSize, _maxPageSize);
    }

    public static PagedResponseDto<T> ToPagedResponse<T>(PagedQueryResult<T> result)
    {
        var totalPages = result.PageSize <= 0 ? 0 : (int)Math.Ceiling((double)result.TotalCount / result.PageSize);
        return new PagedResponseDto<T>(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount,
            totalPages,
            result.Page < totalPages,
            result.Page > 1 && totalPages > 0);
    }
}
