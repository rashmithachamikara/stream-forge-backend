using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Engagement;
using StreamForge.Application.UseCases.Content;
using StreamForge.Application.UseCases.Engagement;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.Controllers;

/// <summary>
/// Exposes authenticated current-user endpoints for content, bookmarks, playlists, and notifications.
/// </summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly ListMyVideosService _listMyVideos;
    private readonly ListMyUploadSessionsService _listMyUploadSessions;
    private readonly ListBookmarksService _listBookmarks;
    private readonly ListMyPlaylistsService _listMyPlaylists;
    private readonly ListNotificationsService _listNotifications;
    private readonly GetUnreadNotificationCountService _getUnreadNotificationCount;
    private readonly MarkNotificationReadStateService _markNotificationReadState;
    private readonly MarkAllNotificationsReadService _markAllNotificationsRead;
    private readonly DeleteNotificationService _deleteNotification;
    private readonly DeleteReadNotificationsService _deleteReadNotifications;

    public MeController(
        ListMyVideosService listMyVideos,
        ListMyUploadSessionsService listMyUploadSessions,
        ListBookmarksService listBookmarks,
        ListMyPlaylistsService listMyPlaylists,
        ListNotificationsService listNotifications,
        GetUnreadNotificationCountService getUnreadNotificationCount,
        MarkNotificationReadStateService markNotificationReadState,
        MarkAllNotificationsReadService markAllNotificationsRead,
        DeleteNotificationService deleteNotification,
        DeleteReadNotificationsService deleteReadNotifications)
    {
        _listMyVideos = listMyVideos;
        _listMyUploadSessions = listMyUploadSessions;
        _listBookmarks = listBookmarks;
        _listMyPlaylists = listMyPlaylists;
        _listNotifications = listNotifications;
        _getUnreadNotificationCount = getUnreadNotificationCount;
        _markNotificationReadState = markNotificationReadState;
        _markAllNotificationsRead = markAllNotificationsRead;
        _deleteNotification = deleteNotification;
        _deleteReadNotifications = deleteReadNotifications;
    }

    /// <summary>
    /// Lists videos owned by the authenticated user.
    /// </summary>
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

    /// <summary>
    /// Lists upload sessions owned by the authenticated user.
    /// </summary>
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

    /// <summary>
    /// Lists bookmarks created by the authenticated user.
    /// </summary>
    [HttpGet("bookmarks")]
    public async Task<ActionResult<PagedResponseDto<BookmarkDto>>> ListBookmarks(
        [FromQuery] Guid? videoId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _listBookmarks.Handle(new ListBookmarksQuery(videoId, page, pageSize), cancellationToken));
    }

    /// <summary>
    /// Lists playlists owned by the authenticated user.
    /// </summary>
    [HttpGet("playlists")]
    public async Task<ActionResult<PagedResponseDto<PlaylistDto>>> ListPlaylists(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _listMyPlaylists.Handle(page, pageSize, cancellationToken));
    }

    /// <summary>
    /// Lists notifications for the authenticated user.
    /// </summary>
    [HttpGet("notifications")]
    public async Task<ActionResult<PagedResponseDto<NotificationDto>>> ListNotifications(
        [FromQuery] bool? isRead,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _listNotifications.Handle(new ListNotificationsQuery(isRead, page, pageSize), cancellationToken));
    }

    /// <summary>
    /// Gets the unread notification count for the authenticated user.
    /// </summary>
    [HttpGet("notifications/unread-count")]
    public async Task<ActionResult<UnreadNotificationCountDto>> GetUnreadCount(CancellationToken cancellationToken)
    {
        return Ok(await _getUnreadNotificationCount.Handle(cancellationToken));
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    [HttpPost("notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await _markNotificationReadState.Handle(notificationId, isRead: true, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Marks a notification as unread.
    /// </summary>
    [HttpPost("notifications/{notificationId:guid}/unread")]
    public async Task<IActionResult> MarkUnread(Guid notificationId, CancellationToken cancellationToken)
    {
        await _markNotificationReadState.Handle(notificationId, isRead: false, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    [HttpPost("notifications/mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _markAllNotificationsRead.Handle(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a single notification.
    /// </summary>
    [HttpDelete("notifications/{notificationId:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid notificationId, CancellationToken cancellationToken)
    {
        await _deleteNotification.Handle(notificationId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes all read notifications for the authenticated user.
    /// </summary>
    [HttpDelete("notifications/read")]
    public async Task<IActionResult> DeleteReadNotifications(CancellationToken cancellationToken)
    {
        await _deleteReadNotifications.Handle(cancellationToken);
        return NoContent();
    }
}
