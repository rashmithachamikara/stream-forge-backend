using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.UseCases.Content;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly GetUserProfileService _getUserProfile;
    private readonly ListUsersService _listUsers;
    private readonly ListVideosService _listVideos;

    public UsersController(
        GetUserProfileService getUserProfile,
        ListUsersService listUsers,
        ListVideosService listVideos)
    {
        _getUserProfile = getUserProfile;
        _listUsers = listUsers;
        _listVideos = listVideos;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponseDto<UserProfileDto>>> List(
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] DateTime? createdTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListUsersQuery(search, role, isActive, page, pageSize, createdFrom, createdTo);
        return Ok(await _listUsers.Handle(query, cancellationToken));
    }

    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileDto>> Get(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _getUserProfile.Handle(userId, cancellationToken));
    }

    [HttpGet("{userId:guid}/videos")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<VideoSummaryDto>>> ListVideos(
        Guid userId,
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? tagId,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new ListVideosQuery(search, categoryId, tagId, userId, null, null, null, sort, page, pageSize);
        return Ok(await _listVideos.Handle(query, cancellationToken));
    }
}
