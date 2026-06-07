using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Analytics;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/me/analytics")]
[Authorize]
public sealed class MeAnalyticsController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly GetMyAnalyticsSummaryService _getSummary;
    private readonly GetRankedVideosAnalyticsService _getRankedVideos;
    private readonly GetViewsOverTimeAnalyticsService _getViewsOverTime;
    private readonly GetDeviceBreakdownAnalyticsService _getDeviceBreakdown;
    private readonly GetBrowserBreakdownAnalyticsService _getBrowserBreakdown;
    private readonly GetAuthBreakdownAnalyticsService _getAuthBreakdown;
    private readonly GetCategoryBreakdownAnalyticsService _getCategoryBreakdown;
    private readonly GetTagBreakdownAnalyticsService _getTagBreakdown;
    private readonly ExportAnalyticsReportService _exportReport;

    public MeAnalyticsController(
        ICurrentUserService currentUserService,
        GetMyAnalyticsSummaryService getSummary,
        GetRankedVideosAnalyticsService getRankedVideos,
        GetViewsOverTimeAnalyticsService getViewsOverTime,
        GetDeviceBreakdownAnalyticsService getDeviceBreakdown,
        GetBrowserBreakdownAnalyticsService getBrowserBreakdown,
        GetAuthBreakdownAnalyticsService getAuthBreakdown,
        GetCategoryBreakdownAnalyticsService getCategoryBreakdown,
        GetTagBreakdownAnalyticsService getTagBreakdown,
        ExportAnalyticsReportService exportReport)
    {
        _currentUserService = currentUserService;
        _getSummary = getSummary;
        _getRankedVideos = getRankedVideos;
        _getViewsOverTime = getViewsOverTime;
        _getDeviceBreakdown = getDeviceBreakdown;
        _getBrowserBreakdown = getBrowserBreakdown;
        _getAuthBreakdown = getAuthBreakdown;
        _getCategoryBreakdown = getCategoryBreakdown;
        _getTagBreakdown = getTagBreakdown;
        _exportReport = exportReport;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getSummary.Handle(from, to, cancellationToken));
    }

    [HttpGet("top-videos")]
    public async Task<ActionResult<PagedResponseDto<RankedVideoAnalyticsDto>>> GetTopVideos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _getRankedVideos.Handle(RequireUserId(), AnalyticsRankingType.MostWatched, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("most-liked-videos")]
    public async Task<ActionResult<PagedResponseDto<RankedVideoAnalyticsDto>>> GetMostLikedVideos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _getRankedVideos.Handle(RequireUserId(), AnalyticsRankingType.MostLiked, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("most-commented-videos")]
    public async Task<ActionResult<PagedResponseDto<RankedVideoAnalyticsDto>>> GetMostCommentedVideos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _getRankedVideos.Handle(RequireUserId(), AnalyticsRankingType.MostCommented, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("most-engaged-videos")]
    public async Task<ActionResult<PagedResponseDto<RankedVideoAnalyticsDto>>> GetMostEngagedVideos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _getRankedVideos.Handle(RequireUserId(), AnalyticsRankingType.MostEngaged, from, to, page, pageSize, cancellationToken));
    }

    [HttpGet("views-over-time")]
    public async Task<ActionResult<IReadOnlyList<AnalyticsTimeSeriesPointDto>>> GetViewsOverTime(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getViewsOverTime.Handle(RequireUserId(), from, to, cancellationToken));
    }

    [HttpGet("device-breakdown")]
    public async Task<ActionResult<IReadOnlyList<DeviceBreakdownItemDto>>> GetDeviceBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getDeviceBreakdown.Handle(RequireUserId(), from, to, cancellationToken));
    }

    [HttpGet("browser-breakdown")]
    public async Task<ActionResult<IReadOnlyList<BrowserBreakdownItemDto>>> GetBrowserBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getBrowserBreakdown.Handle(RequireUserId(), from, to, cancellationToken));
    }

    [HttpGet("auth-breakdown")]
    public async Task<ActionResult<AuthBreakdownDto>> GetAuthBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getAuthBreakdown.Handle(RequireUserId(), from, to, cancellationToken));
    }

    [HttpGet("category-breakdown")]
    public async Task<ActionResult<IReadOnlyList<CategoryBreakdownItemDto>>> GetCategoryBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getCategoryBreakdown.Handle(RequireUserId(), from, to, cancellationToken));
    }

    [HttpGet("tag-breakdown")]
    public async Task<ActionResult<IReadOnlyList<TagBreakdownItemDto>>> GetTagBreakdown(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getTagBreakdown.Handle(RequireUserId(), from, to, cancellationToken));
    }

    [HttpGet("reports/videos")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only csv format is supported", nameof(format));
        }

        var report = await _exportReport.Handle(RequireUserId(), "my-video-analytics", from, to, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    private Guid RequireUserId() => _currentUserService.UserId ?? throw new System.UnauthorizedAccessException("User must be authenticated");
}
