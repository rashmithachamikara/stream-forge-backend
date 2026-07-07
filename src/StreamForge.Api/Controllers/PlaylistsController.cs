using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Engagement;
using StreamForge.Application.UseCases.Engagement;

namespace StreamForge.Api.Controllers;

/// <summary>
/// Exposes playlist management and playlist-video membership endpoints.
/// </summary>
[ApiController]
[Route("api/v1/playlists")]
public sealed class PlaylistsController : ControllerBase
{
    private readonly ListPlaylistsService _listPlaylists;
    private readonly CreatePlaylistService _createPlaylist;
    private readonly GetPlaylistService _getPlaylist;
    private readonly UpdatePlaylistService _updatePlaylist;
    private readonly DeletePlaylistService _deletePlaylist;
    private readonly GetPlaylistVideosService _getPlaylistVideos;
    private readonly AddPlaylistVideoService _addPlaylistVideo;
    private readonly RemovePlaylistVideoService _removePlaylistVideo;
    private readonly ReorderPlaylistVideosService _reorderPlaylistVideos;

    public PlaylistsController(
        ListPlaylistsService listPlaylists,
        CreatePlaylistService createPlaylist,
        GetPlaylistService getPlaylist,
        UpdatePlaylistService updatePlaylist,
        DeletePlaylistService deletePlaylist,
        GetPlaylistVideosService getPlaylistVideos,
        AddPlaylistVideoService addPlaylistVideo,
        RemovePlaylistVideoService removePlaylistVideo,
        ReorderPlaylistVideosService reorderPlaylistVideos)
    {
        _listPlaylists = listPlaylists;
        _createPlaylist = createPlaylist;
        _getPlaylist = getPlaylist;
        _updatePlaylist = updatePlaylist;
        _deletePlaylist = deletePlaylist;
        _getPlaylistVideos = getPlaylistVideos;
        _addPlaylistVideo = addPlaylistVideo;
        _removePlaylistVideo = removePlaylistVideo;
        _reorderPlaylistVideos = reorderPlaylistVideos;
    }

    /// <summary>
    /// Lists playlists, optionally filtered by owner.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<PlaylistDto>>> List(
        [FromQuery] Guid? ownerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _listPlaylists.Handle(new ListPlaylistsQuery(ownerId, page, pageSize), cancellationToken));
    }

    /// <summary>
    /// Creates a new playlist for the authenticated user.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PlaylistDto>> Create(
        [FromBody] CreatePlaylistRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _createPlaylist.Handle(request, cancellationToken));
    }

    /// <summary>
    /// Gets a single playlist by identifier.
    /// </summary>
    [HttpGet("{playlistId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PlaylistDto>> Get(Guid playlistId, CancellationToken cancellationToken)
    {
        return Ok(await _getPlaylist.Handle(playlistId, cancellationToken));
    }

    /// <summary>
    /// Updates a playlist owned by the authenticated user.
    /// </summary>
    [HttpPatch("{playlistId:guid}")]
    [Authorize]
    public async Task<ActionResult<PlaylistDto>> Update(
        Guid playlistId,
        [FromBody] UpdatePlaylistRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updatePlaylist.Handle(playlistId, request, cancellationToken));
    }

    /// <summary>
    /// Deletes a playlist owned by the authenticated user.
    /// </summary>
    [HttpDelete("{playlistId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid playlistId, CancellationToken cancellationToken)
    {
        await _deletePlaylist.Handle(playlistId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets the paged videos contained in a playlist.
    /// </summary>
    [HttpGet("{playlistId:guid}/videos")]
    [AllowAnonymous]
    public async Task<ActionResult<PlaylistVideosPageDto>> GetVideos(
        Guid playlistId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _getPlaylistVideos.Handle(playlistId, page, pageSize, cancellationToken));
    }

    /// <summary>
    /// Adds a video to a playlist.
    /// </summary>
    [HttpPost("{playlistId:guid}/videos")]
    [Authorize]
    public async Task<IActionResult> AddVideo(
        Guid playlistId,
        [FromBody] AddPlaylistVideoRequestDto request,
        CancellationToken cancellationToken)
    {
        await _addPlaylistVideo.Handle(playlistId, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Removes a video from a playlist.
    /// </summary>
    [HttpDelete("{playlistId:guid}/videos/{videoId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveVideo(Guid playlistId, Guid videoId, CancellationToken cancellationToken)
    {
        await _removePlaylistVideo.Handle(playlistId, videoId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reorders the videos inside a playlist.
    /// </summary>
    [HttpPost("{playlistId:guid}/videos/reorder")]
    [Authorize]
    public async Task<IActionResult> ReorderVideos(
        Guid playlistId,
        [FromBody] ReorderPlaylistVideosRequestDto request,
        CancellationToken cancellationToken)
    {
        await _reorderPlaylistVideos.Handle(playlistId, request, cancellationToken);
        return NoContent();
    }
}
