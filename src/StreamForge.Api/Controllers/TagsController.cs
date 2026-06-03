using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.UseCases.Content;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/tags")]
[AllowAnonymous]
public sealed class TagsController : ControllerBase
{
    private readonly ListTagsService _listTags;
    private readonly GetTagService _getTag;
    private readonly ListVideosService _listVideos;

    public TagsController(
        ListTagsService listTags,
        GetTagService getTag,
        ListVideosService listVideos)
    {
        _listTags = listTags;
        _getTag = getTag;
        _listVideos = listVideos;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<TagSummaryDto>>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new ListTagsQuery(search, page, pageSize);
        return Ok(await _listTags.Handle(query, cancellationToken));
    }

    [HttpGet("{tagId:guid}")]
    public async Task<ActionResult<TagSummaryDto>> Get(Guid tagId, CancellationToken cancellationToken)
    {
        return Ok(await _getTag.Handle(tagId, cancellationToken));
    }

    [HttpGet("{tagId:guid}/videos")]
    public async Task<ActionResult<PagedResponseDto<VideoSummaryDto>>> ListVideos(
        Guid tagId,
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? uploaderId,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        await _getTag.Handle(tagId, cancellationToken);
        var query = new ListVideosQuery(search, categoryId, tagId, uploaderId, null, null, sort, page, pageSize);
        return Ok(await _listVideos.Handle(query, cancellationToken));
    }
}
