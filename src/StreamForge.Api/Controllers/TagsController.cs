using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.UseCases.Content;

namespace StreamForge.Api.Controllers;

/// <summary>
/// Exposes tag management and tag-scoped video listing endpoints.
/// </summary>
[ApiController]
[Route("api/v1/tags")]
public sealed class TagsController : ControllerBase
{
    private readonly ListTagsService _listTags;
    private readonly GetTagService _getTag;
    private readonly CreateTagService _createTag;
    private readonly UpdateTagService _updateTag;
    private readonly DeleteTagService _deleteTag;
    private readonly ListVideosService _listVideos;

    public TagsController(
        ListTagsService listTags,
        GetTagService getTag,
        CreateTagService createTag,
        UpdateTagService updateTag,
        DeleteTagService deleteTag,
        ListVideosService listVideos)
    {
        _listTags = listTags;
        _getTag = getTag;
        _createTag = createTag;
        _updateTag = updateTag;
        _deleteTag = deleteTag;
        _listVideos = listVideos;
    }

    /// <summary>
    /// Lists tags with optional search and paging.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<TagSummaryDto>>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new ListTagsQuery(search, page, pageSize);
        return Ok(await _listTags.Handle(query, cancellationToken));
    }

    /// <summary>
    /// Gets a single tag by identifier.
    /// </summary>
    [HttpGet("{tagId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TagSummaryDto>> Get(Guid tagId, CancellationToken cancellationToken)
    {
        return Ok(await _getTag.Handle(tagId, cancellationToken));
    }

    /// <summary>
    /// Creates a new tag.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TagSummaryDto>> Create(
        [FromBody] CreateTagRequestDto request,
        CancellationToken cancellationToken)
    {
        var created = await _createTag.Handle(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { tagId = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing tag.
    /// </summary>
    [HttpPatch("{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TagSummaryDto>> Update(
        Guid tagId,
        [FromBody] UpdateTagRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateTag.Handle(tagId, request, cancellationToken));
    }

    /// <summary>
    /// Deletes a tag.
    /// </summary>
    [HttpDelete("{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid tagId, CancellationToken cancellationToken)
    {
        await _deleteTag.Handle(tagId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists videos currently associated with a tag.
    /// </summary>
    [HttpGet("{tagId:guid}/videos")]
    [AllowAnonymous]
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
        var query = new ListVideosQuery(search, categoryId, tagId, uploaderId, null, null, null, sort, page, pageSize);
        return Ok(await _listVideos.Handle(query, cancellationToken));
    }
}
