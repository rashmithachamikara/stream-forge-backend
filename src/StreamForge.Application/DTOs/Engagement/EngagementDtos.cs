using StreamForge.Application.DTOs.Content;
using StreamForge.Domain.Enums;

namespace StreamForge.Application.DTOs.Engagement;

public sealed record ReactionSummaryDto(
    Guid VideoId,
    int LikeCount,
    int DislikeCount,
    ReactionType? CurrentUserReaction);

public sealed record SetReactionRequestDto(ReactionType ReactionType);

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

public sealed record CreateCommentRequestDto(
    string Comment,
    Guid? ParentCommentId);

public sealed record UpdateCommentRequestDto(string Comment);

public sealed record BookmarkDto(
    Guid Id,
    Guid VideoId,
    int TimestampSeconds,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    VideoSummaryDto? Video);

public sealed record CreateBookmarkRequestDto(
    int TimestampSeconds,
    string? Note);

public sealed record UpdateBookmarkRequestDto(
    int TimestampSeconds,
    string? Note);

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

public sealed record CreatePlaylistRequestDto(
    string Name,
    string? Description,
    PlaylistVisibility Visibility);

public sealed record UpdatePlaylistRequestDto(
    string Name,
    string? Description,
    PlaylistVisibility Visibility);

public sealed record AddPlaylistVideoRequestDto(
    Guid VideoId,
    int? OrderIndex);

public sealed record ReorderPlaylistVideosRequestDto(
    IReadOnlyList<Guid> VideoIds);

public sealed record NotificationDto(
    Guid Id,
    Guid? VideoId,
    NotificationType NotificationType,
    string Message,
    bool IsRead,
    DateTime CreatedAt);

public sealed record UnreadNotificationCountDto(int UnreadCount);

public sealed record PlaylistVideosPageDto(
    PlaylistDto Playlist,
    PagedResponseDto<VideoSummaryDto> Videos);
