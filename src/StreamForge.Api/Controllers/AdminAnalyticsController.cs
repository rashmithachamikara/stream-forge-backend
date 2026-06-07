using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.UseCases.Analytics;
using StreamForge.Domain.Exceptions;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/admin/analytics")]
[Authorize(Roles = "Admin")]
public sealed class AdminAnalyticsController : ControllerBase
{
    private readonly GetAdminAnalyticsSummaryService _getSummary;
    private readonly GetRankedVideosAnalyticsService _getRankedVideos;
    private readonly GetViewsOverTimeAnalyticsService _getViewsOverTime;
    private readonly GetActiveViewersAnalyticsService _getActiveViewers;
    private readonly GetPeakWatchTimeAnalyticsService _getPeakWatchTime;
    private readonly GetDeviceBreakdownAnalyticsService _getDeviceBreakdown;
    private readonly GetBrowserBreakdownAnalyticsService _getBrowserBreakdown;
    private readonly GetAuthBreakdownAnalyticsService _getAuthBreakdown;
    private readonly GetCategoryBreakdownAnalyticsService _getCategoryBreakdown;
    private readonly GetTagBreakdownAnalyticsService _getTagBreakdown;
    private readonly ExportAnalyticsReportService _exportReport;
    private readonly AnalyticsOptions _options;

    public AdminAnalyticsController(
        GetAdminAnalyticsSummaryService getSummary,
        GetRankedVideosAnalyticsService getRankedVideos,
        GetViewsOverTimeAnalyticsService getViewsOverTime,
        GetActiveViewersAnalyticsService getActiveViewers,
        GetPeakWatchTimeAnalyticsService getPeakWatchTime,
        GetDeviceBreakdownAnalyticsService getDeviceBreakdown,
        GetBrowserBreakdownAnalyticsService getBrowserBreakdown,
        GetAuthBreakdownAnalyticsService getAuthBreakdown,
        GetCategoryBreakdownAnalyticsService getCategoryBreakdown,
        GetTagBreakdownAnalyticsService getTagBreakdown,
        ExportAnalyticsReportService exportReport,
        AnalyticsOptions options)
    {
        _getSummary = getSummary;
        _getRankedVideos = getRankedVideos;
        _getViewsOverTime = getViewsOverTime;
        _getActiveViewers = getActiveViewers;
        _getPeakWatchTime = getPeakWatchTime;
        _getDeviceBreakdown = getDeviceBreakdown;
        _getBrowserBreakdown = getBrowserBreakdown;
        _getAuthBreakdown = getAuthBreakdown;
        _getCategoryBreakdown = getCategoryBreakdown;
        _getTagBreakdown = getTagBreakdown;
        _exportReport = exportReport;
        _options = options;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getSummary.Handle(from, to, cancellationToken));
    }

    [HttpGet("views-over-time")]
    public async Task<ActionResult<IReadOnlyList<AnalyticsTimeSeriesPointDto>>> GetViewsOverTime(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getViewsOverTime.Handle(ownerId: null, from, to, cancellationToken));
    }

    [HttpGet("most-watched-videos")]
    public async Task<ActionResult<PagedResponseDto<RankedVideoAnalyticsDto>>> GetMostWatchedVideos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getRankedVideos.Handle(ownerId: null, AnalyticsRankingType.MostWatched, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("most-liked-videos")]
    public async Task<ActionResult<PagedResponseDto<RankedVideoAnalyticsDto>>> GetMostLikedVideos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getRankedVideos.Handle(ownerId: null, AnalyticsRankingType.MostLiked, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("most-commented-videos")]
    public async Task<ActionResult<PagedResponseDto<RankedVideoAnalyticsDto>>> GetMostCommentedVideos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getRankedVideos.Handle(ownerId: null, AnalyticsRankingType.MostCommented, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("most-engaged-videos")]
    public async Task<ActionResult<PagedResponseDto<RankedVideoAnalyticsDto>>> GetMostEngagedVideos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getRankedVideos.Handle(ownerId: null, AnalyticsRankingType.MostEngaged, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("active-viewers")]
    public async Task<ActionResult<ActiveViewersDto>> GetActiveViewers(CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getActiveViewers.Handle(ownerId: null, cancellationToken));
    }

    [HttpGet("peak-watch-time")]
    public async Task<ActionResult<IReadOnlyList<PeakWatchTimeItemDto>>> GetPeakWatchTime(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getPeakWatchTime.Handle(ownerId: null, from, to, cancellationToken));
    }

    [HttpGet("device-breakdown")]
    public async Task<ActionResult<IReadOnlyList<DeviceBreakdownItemDto>>> GetDeviceBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getDeviceBreakdown.Handle(ownerId: null, from, to, cancellationToken));
    }

    [HttpGet("browser-breakdown")]
    public async Task<ActionResult<IReadOnlyList<BrowserBreakdownItemDto>>> GetBrowserBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getBrowserBreakdown.Handle(ownerId: null, from, to, cancellationToken));
    }

    [HttpGet("auth-breakdown")]
    public async Task<ActionResult<AuthBreakdownDto>> GetAuthBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getAuthBreakdown.Handle(ownerId: null, from, to, cancellationToken));
    }

    [HttpGet("category-breakdown")]
    public async Task<ActionResult<IReadOnlyList<CategoryBreakdownItemDto>>> GetCategoryBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getCategoryBreakdown.Handle(ownerId: null, from, to, cancellationToken));
    }

    [HttpGet("tag-breakdown")]
    public async Task<ActionResult<IReadOnlyList<TagBreakdownItemDto>>> GetTagBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        EnsureAdminReportingEnabled();
        return Ok(await _getTagBreakdown.Handle(ownerId: null, from, to, cancellationToken));
    }

    [HttpGet("reports/overview")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        EnsureAdminReportingEnabled();
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only csv format is supported", nameof(format));
        }

        var report = await _exportReport.Handle(ownerId: null, "platform-analytics", from, to, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    private void EnsureAdminReportingEnabled()
    {
        if (!_options.Enabled || !_options.AdminReportingEnabled)
        {
            throw new BusinessRuleViolationException("Admin analytics reporting is disabled");
        }
    }
}
