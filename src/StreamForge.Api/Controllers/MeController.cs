using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.UseCases.Content;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly ListMyVideosService _listMyVideos;
    private readonly ListMyUploadSessionsService _listMyUploadSessions;

    public MeController(
        ListMyVideosService listMyVideos,
        ListMyUploadSessionsService listMyUploadSessions)
    {
        _listMyVideos = listMyVideos;
        _listMyUploadSessions = listMyUploadSessions;
    }

    [HttpGet("videos")]
    public async Task<ActionResult<PagedResponseDto<VideoSummaryDto>>> ListVideos(
        [FromQuery] VideoStatus? status,
        [FromQuery] VideoVisibility? visibility,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new ListMyVideosQuery(status, visibility, search, sort, page, pageSize);
        return Ok(await _listMyVideos.Handle(query, cancellationToken));
    }

    [HttpGet("upload-sessions")]
    public async Task<ActionResult<PagedResponseDto<UploadSessionSummaryDto>>> ListUploadSessions(
        [FromQuery] UploadSessionStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new ListMyUploadSessionsQuery(status, page, pageSize);
        return Ok(await _listMyUploadSessions.Handle(query, cancellationToken));
    }
}
