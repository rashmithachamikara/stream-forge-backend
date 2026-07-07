using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.UseCases.Content;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.Controllers.Videos;

/// <summary>
/// Exposes core video listing, detail, metadata update, processing, and access-management endpoints.
/// </summary>
[ApiController]
[Route("api/v1/videos")]
public sealed class VideosController : ControllerBase
{
    private readonly ListVideosService _listVideos;
    private readonly GetVideoDetailsService _getVideoDetails;
    private readonly UpdateVideoService _updateVideo;
    private readonly ArchiveVideoService _archiveVideo;
    private readonly GetVideoProcessingStatusService _getProcessingStatus;
    private readonly ListVideoAccessGrantsService _listAccessGrants;
    private readonly CreateVideoAccessGrantService _createAccessGrant;
    private readonly RevokeVideoAccessGrantService _revokeAccessGrant;

    public VideosController(
        ListVideosService listVideos,
        GetVideoDetailsService getVideoDetails,
        UpdateVideoService updateVideo,
        ArchiveVideoService archiveVideo,
        GetVideoProcessingStatusService getProcessingStatus,
        ListVideoAccessGrantsService listAccessGrants,
        CreateVideoAccessGrantService createAccessGrant,
        RevokeVideoAccessGrantService revokeAccessGrant)
    {
        _listVideos = listVideos;
        _getVideoDetails = getVideoDetails;
        _updateVideo = updateVideo;
        _archiveVideo = archiveVideo;
        _getProcessingStatus = getProcessingStatus;
        _listAccessGrants = listAccessGrants;
        _createAccessGrant = createAccessGrant;
        _revokeAccessGrant = revokeAccessGrant;
    }

    /// <summary>
    /// Lists videos using the supplied filters, sorting, and paging options.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<VideoSummaryDto>>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? tagId,
        [FromQuery] Guid? uploaderId,
        [FromQuery] Guid? excludeUploaderId,
        [FromQuery] VideoStatus? status,
        [FromQuery] VideoVisibility? visibility,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] DateTime? createdTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListVideosQuery(search, categoryId, tagId, uploaderId, excludeUploaderId, status, visibility, sort, page, pageSize, createdFrom, createdTo);
        return Ok(await _listVideos.Handle(query, cancellationToken));
    }

    /// <summary>
    /// Gets detailed metadata for a single video.
    /// </summary>
    [HttpGet("{videoId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<VideoDetailDto>> Get(
        Guid videoId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _getVideoDetails.Handle(videoId, shareToken, cancellationToken));
    }

    /// <summary>
    /// Gets the current processing status for a video visible to the authenticated caller.
    /// </summary>
    [HttpGet("{videoId:guid}/processing-status")]
    [Authorize]
    public async Task<ActionResult<VideoProcessingStatusDetailsDto>> GetProcessingStatus(
        Guid videoId,
        CancellationToken cancellationToken)
    {
        return Ok(await _getProcessingStatus.Handle(videoId, cancellationToken));
    }

    /// <summary>
    /// Updates editable metadata and player settings for a video.
    /// </summary>
    [HttpPatch("{videoId:guid}")]
    [Authorize]
    public async Task<ActionResult<VideoDetailDto>> Update(
        Guid videoId,
        [FromBody] UpdateVideoRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateVideo.Handle(videoId, request, cancellationToken));
    }

    /// <summary>
    /// Archives a video without permanently deleting its historical record.
    /// </summary>
    [HttpPost("{videoId:guid}/archive")]
    [Authorize]
    public async Task<IActionResult> Archive(Guid videoId, CancellationToken cancellationToken)
    {
        await _archiveVideo.Handle(videoId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a video by routing through the archive workflow.
    /// </summary>
    [HttpDelete("{videoId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid videoId, CancellationToken cancellationToken)
    {
        await _archiveVideo.Handle(videoId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists access grants defined for a video.
    /// </summary>
    [HttpGet("{videoId:guid}/access")]
    [Authorize]
    public async Task<ActionResult<PagedResponseDto<AccessGrantDto>>> ListAccessGrants(
        Guid videoId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new ListVideoAccessGrantsQuery(videoId, isActive, page, pageSize);
        return Ok(await _listAccessGrants.Handle(query, cancellationToken));
    }

    /// <summary>
    /// Creates a direct user or share-token access grant for a video.
    /// </summary>
    [HttpPost("{videoId:guid}/access")]
    [Authorize]
    public async Task<ActionResult<AccessGrantDto>> CreateAccessGrant(
        Guid videoId,
        [FromBody] CreateAccessGrantRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _createAccessGrant.Handle(videoId, request, cancellationToken));
    }

    /// <summary>
    /// Revokes an access grant from a video.
    /// </summary>
    [HttpDelete("{videoId:guid}/access/{accessControlId:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeAccessGrant(
        Guid videoId,
        Guid accessControlId,
        CancellationToken cancellationToken)
    {
        await _revokeAccessGrant.Handle(videoId, accessControlId, cancellationToken);
        return NoContent();
    }
}
