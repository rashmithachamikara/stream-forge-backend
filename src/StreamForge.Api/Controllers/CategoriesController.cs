using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.UseCases.Content;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ListCategoriesService _listCategories;
    private readonly GetCategoryService _getCategory;
    private readonly CreateCategoryService _createCategory;
    private readonly UpdateCategoryService _updateCategory;
    private readonly DeleteCategoryService _deleteCategory;
    private readonly ListVideosService _listVideos;

    public CategoriesController(
        ListCategoriesService listCategories,
        GetCategoryService getCategory,
        CreateCategoryService createCategory,
        UpdateCategoryService updateCategory,
        DeleteCategoryService deleteCategory,
        ListVideosService listVideos)
    {
        _listCategories = listCategories;
        _getCategory = getCategory;
        _createCategory = createCategory;
        _updateCategory = updateCategory;
        _deleteCategory = deleteCategory;
        _listVideos = listVideos;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _listCategories.Handle(cancellationToken));
    }

    [HttpGet("{categoryId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<CategoryDto>> Get(Guid categoryId, CancellationToken cancellationToken)
    {
        return Ok(await _getCategory.Handle(categoryId, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var created = await _createCategory.Handle(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { categoryId = created.Id }, created);
    }

    [HttpPatch("{categoryId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Update(
        Guid categoryId,
        [FromBody] UpdateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateCategory.Handle(categoryId, request, cancellationToken));
    }

    [HttpDelete("{categoryId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        await _deleteCategory.Handle(categoryId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{categoryId:guid}/videos")]
    [AllowAnonymous]
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
        var query = new ListVideosQuery(search, categoryId, tagId, uploaderId, null, null, null, sort, page, pageSize);
        return Ok(await _listVideos.Handle(query, cancellationToken));
    }
}
