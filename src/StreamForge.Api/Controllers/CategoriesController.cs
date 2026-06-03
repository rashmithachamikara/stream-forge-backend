using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.UseCases.Content;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[AllowAnonymous]
public sealed class CategoriesController : ControllerBase
{
    private readonly ListCategoriesService _listCategories;
    private readonly GetCategoryService _getCategory;
    private readonly ListVideosService _listVideos;

    public CategoriesController(
        ListCategoriesService listCategories,
        GetCategoryService getCategory,
        ListVideosService listVideos)
    {
        _listCategories = listCategories;
        _getCategory = getCategory;
        _listVideos = listVideos;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _listCategories.Handle(cancellationToken));
    }

    [HttpGet("{categoryId:guid}")]
    public async Task<ActionResult<CategoryDto>> Get(Guid categoryId, CancellationToken cancellationToken)
    {
        return Ok(await _getCategory.Handle(categoryId, cancellationToken));
    }

    [HttpGet("{categoryId:guid}/videos")]
    public async Task<ActionResult<PagedResponseDto<VideoSummaryDto>>> ListVideos(
        Guid categoryId,
        [FromQuery] string? search,
        [FromQuery] Guid? tagId,
        [FromQuery] Guid? uploaderId,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        await _getCategory.Handle(categoryId, cancellationToken);
        var query = new ListVideosQuery(search, categoryId, tagId, uploaderId, null, null, sort, page, pageSize);
        return Ok(await _listVideos.Handle(query, cancellationToken));
    }
}
