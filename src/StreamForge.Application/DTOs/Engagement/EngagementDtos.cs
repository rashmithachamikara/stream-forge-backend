using StreamForge.Application.DTOs.Content;
using StreamForge.Domain.Enums;

namespace StreamForge.Application.DTOs.Engagement;

/// <summary>
/// Represents aggregated reaction counts for a video and the current user's reaction state.
/// </summary>
public sealed record ReactionSummaryDto(
    Guid VideoId,
    int LikeCount,
    int DislikeCount,
    ReactionType? CurrentUserReaction);

/// <summary>
/// Represents a request to set the authenticated user's reaction on a video.
/// </summary>
public sealed record SetReactionRequestDto(ReactionType ReactionType);

/// <summary>
/// Represents a comment attached to a video.
/// </summary>
public sealed record CommentDto(
    Guid Id,
    Guid VideoId,
    Guid UserId,
    string UserName,
    Guid? ParentCommentId,
    string Comment,
    int ReplyCount,
    bool IsEdited,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Represents a request to create a video comment or reply.
/// </summary>
public sealed record CreateCommentRequestDto(
    string Comment,
    Guid? ParentCommentId);

/// <summary>
/// Represents a request to update an existing comment.
/// </summary>
public sealed record UpdateCommentRequestDto(string Comment);

/// <summary>
/// Represents a saved bookmark for a video timestamp.
/// </summary>
public sealed record BookmarkDto(
    Guid Id,
    Guid VideoId,
    int TimestampSeconds,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    VideoSummaryDto? Video);

/// <summary>
/// Represents a request to create a bookmark on a video.
/// </summary>
public sealed record CreateBookmarkRequestDto(
    int TimestampSeconds,
    string? Note);

/// <summary>
/// Represents a request to update an existing bookmark.
/// </summary>
public sealed record UpdateBookmarkRequestDto(
    int TimestampSeconds,
    string? Note);

/// <summary>
/// Represents a playlist with ownership and visibility metadata.
/// </summary>
public sealed record PlaylistDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerName,
    PlaylistVisibility Visibility,
    int VideoCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Represents a request to create a playlist.
/// </summary>
public sealed record CreatePlaylistRequestDto(
    string Name,
    string? Description,
    PlaylistVisibility Visibility);

/// <summary>
/// Represents a request to update a playlist.
/// </summary>
public sealed record UpdatePlaylistRequestDto(
    string Name,
    string? Description,
    PlaylistVisibility Visibility);

/// <summary>
/// Represents a request to add a video to a playlist.
/// </summary>
public sealed record AddPlaylistVideoRequestDto(
    Guid VideoId,
    int? OrderIndex);

/// <summary>
/// Represents a request to reorder videos inside a playlist.
/// </summary>
public sealed record ReorderPlaylistVideosRequestDto(
    IReadOnlyList<Guid> VideoIds);

/// <summary>
/// Represents a notification delivered to a user.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    Guid? VideoId,
    NotificationType NotificationType,
    string Message,
    bool IsRead,
    DateTime CreatedAt);

/// <summary>
/// Represents the current unread notification count for a user.
/// </summary>
public sealed record UnreadNotificationCountDto(int UnreadCount);

/// <summary>
/// Represents a paged playlist view together with its parent playlist metadata.
/// </summary>
public sealed record PlaylistVideosPageDto(
    PlaylistDto Playlist,
    PagedResponseDto<VideoSummaryDto> Videos);
