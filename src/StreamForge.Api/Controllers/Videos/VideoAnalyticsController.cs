using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.UseCases.Analytics;

namespace StreamForge.Api.Controllers.Videos;

[ApiController]
[Route("api/v1/videos/{videoId:guid}/analytics")]
public sealed class VideoAnalyticsController : ControllerBase
{
    private readonly RecordAnalyticsEventService _recordAnalyticsEvent;
    private readonly GetVideoAnalyticsSummaryService _getSummary;
    private readonly GetVideoAnalyticsTimeSeriesService _getTimeSeries;
    private readonly GetVideoAnalyticsEngagementService _getEngagement;

    public VideoAnalyticsController(
        RecordAnalyticsEventService recordAnalyticsEvent,
        GetVideoAnalyticsSummaryService getSummary,
        GetVideoAnalyticsTimeSeriesService getTimeSeries,
        GetVideoAnalyticsEngagementService getEngagement)
    {
        _recordAnalyticsEvent = recordAnalyticsEvent;
        _getSummary = getSummary;
        _getTimeSeries = getTimeSeries;
        _getEngagement = getEngagement;
    }

    [HttpPost("events")]
    [AllowAnonymous]
    public async Task<ActionResult<RecordAnalyticsEventResultDto>> RecordEvent(
        Guid videoId,
        [FromBody] RecordAnalyticsEventRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _recordAnalyticsEvent.Handle(videoId, request, cancellationToken));
    }

    [HttpGet("summary")]
    [Authorize]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(
        Guid videoId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getSummary.Handle(videoId, from, to, cancellationToken));
    }

    [HttpGet("timeseries")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<AnalyticsTimeSeriesPointDto>>> GetTimeSeries(
        Guid videoId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getTimeSeries.Handle(videoId, from, to, cancellationToken));
    }

    [HttpGet("engagement")]
    [Authorize]
    public async Task<ActionResult<AnalyticsEngagementSummaryDto>> GetEngagement(
        Guid videoId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _getEngagement.Handle(videoId, from, to, cancellationToken));
    }
}
