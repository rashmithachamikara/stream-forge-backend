using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Engagement;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Content;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Engagement;

public sealed record ListCommentsQuery(Guid VideoId, Guid? ParentCommentId, int Page, int PageSize);
public sealed record ListPlaylistsQuery(Guid? OwnerId, int Page, int PageSize);
public sealed record ListNotificationsQuery(bool? IsRead, int Page, int PageSize);

public sealed class GetReactionSummaryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public GetReactionSummaryService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<ReactionSummaryDto> Handle(Guid videoId, string? shareToken, CancellationToken cancellationToken)
    {
        await EngagementGuards.EnsureCanViewReadyVideoAsync(
            _unitOfWork,
            _authorizationService,
            _currentUserService.UserId,
            _currentUserService.Role,
            videoId,
            shareToken,
            cancellationToken);

        var summary = await _unitOfWork.VideoReactions.GetSummaryAsync(videoId, _currentUserService.UserId, cancellationToken);
        return new ReactionSummaryDto(videoId, summary.LikeCount, summary.DislikeCount, summary.CurrentUserReaction);
    }
}

public sealed class SetReactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public SetReactionService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<ReactionSummaryDto> Handle(Guid videoId, SetReactionRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");

        var video = await EngagementGuards.EnsureCanEngageWithVideoAsync(
            _unitOfWork,
            _authorizationService,
            _currentUserService.UserId,
            _currentUserService.Role,
            videoId,
            allowComments: false,
            allowLikes: true,
            allowBookmarks: false,
            cancellationToken);

        var existing = await _unitOfWork.VideoReactions.GetByUserAndVideoAsync(userId, videoId, cancellationToken);
        var wasLike = existing?.ReactionType == ReactionType.Like;

        if (existing is null)
        {
            await _unitOfWork.VideoReactions.AddAsync(VideoReaction.Create(userId, videoId, request.ReactionType), cancellationToken);
        }
        else if (existing.ReactionType != request.ReactionType)
        {
            existing.ChangeReaction(request.ReactionType);
            await _unitOfWork.VideoReactions.UpdateAsync(existing, cancellationToken);
        }

        if (request.ReactionType == ReactionType.Like && !wasLike && video.UploaderId != userId)
        {
            await _unitOfWork.Notifications.AddAsync(
                Notification.Create(video.UploaderId, NotificationType.Like, $"{_currentUserService.Name ?? "Someone"} liked your video \"{video.Title}\".", video.Id),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var summary = await _unitOfWork.VideoReactions.GetSummaryAsync(videoId, userId, cancellationToken);
        return new ReactionSummaryDto(videoId, summary.LikeCount, summary.DislikeCount, summary.CurrentUserReaction);
    }
}

public sealed class RemoveReactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveReactionService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ReactionSummaryDto> Handle(Guid videoId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");

        var existing = await _unitOfWork.VideoReactions.GetByUserAndVideoAsync(userId, videoId, cancellationToken);
        if (existing is not null)
        {
            await _unitOfWork.VideoReactions.DeleteAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var summary = await _unitOfWork.VideoReactions.GetSummaryAsync(videoId, userId, cancellationToken);
        return new ReactionSummaryDto(videoId, summary.LikeCount, summary.DislikeCount, summary.CurrentUserReaction);
    }
}

public sealed class ListCommentsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public ListCommentsService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<PagedResponseDto<CommentDto>> Handle(ListCommentsQuery query, string? shareToken, CancellationToken cancellationToken)
    {
        await EngagementGuards.EnsureCanViewReadyVideoAsync(
            _unitOfWork,
            _authorizationService,
            _currentUserService.UserId,
            _currentUserService.Role,
            query.VideoId,
            shareToken,
            cancellationToken);

        var page = Pagination.NormalizePage(query.Page);
        var pageSize = Pagination.NormalizePageSize(query.PageSize);
        var result = await _unitOfWork.VideoComments.GetPagedByVideoIdAsync(query.VideoId, query.ParentCommentId, page, pageSize, cancellationToken);
        return Pagination.Map(result, EngagementMapper.ToComment);
    }
}

public sealed class CreateCommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public CreateCommentService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<CommentDto> Handle(Guid videoId, CreateCommentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");

        var video = await EngagementGuards.EnsureCanEngageWithVideoAsync(
            _unitOfWork,
            _authorizationService,
            _currentUserService.UserId,
            _currentUserService.Role,
            videoId,
            allowComments: true,
            allowLikes: false,
            allowBookmarks: false,
            cancellationToken);

        VideoComment? parentComment = null;
        if (request.ParentCommentId.HasValue)
        {
            parentComment = await _unitOfWork.VideoComments.GetByIdAsync(request.ParentCommentId.Value, cancellationToken)
                ?? throw new EntityNotFoundException("VideoComment", request.ParentCommentId.Value);
            if (parentComment.VideoId != videoId)
            {
                throw new InvalidOperationException("Reply parent comment must belong to the same video");
            }
        }

        var comment = VideoComment.Create(userId, videoId, request.Comment, request.ParentCommentId);
        await _unitOfWork.VideoComments.AddAsync(comment, cancellationToken);

        if (parentComment is not null)
        {
            if (parentComment.UserId != userId)
            {
                await _unitOfWork.Notifications.AddAsync(
                    Notification.Create(parentComment.UserId, NotificationType.Reply, $"{_currentUserService.Name ?? "Someone"} replied to your comment on \"{video.Title}\".", videoId),
                    cancellationToken);
            }
        }
        else if (video.UploaderId != userId)
        {
            await _unitOfWork.Notifications.AddAsync(
                Notification.Create(video.UploaderId, NotificationType.Comment, $"{_currentUserService.Name ?? "Someone"} commented on your video \"{video.Title}\".", videoId),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var created = await _unitOfWork.VideoComments.GetByIdWithUserAsync(comment.Id, cancellationToken)
            ?? throw new EntityNotFoundException("VideoComment", comment.Id);
        return EngagementMapper.ToComment(created);
    }
}

public sealed class UpdateCommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCommentService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CommentDto> Handle(Guid videoId, Guid commentId, UpdateCommentRequestDto request, CancellationToken cancellationToken)
    {
        var comment = await _unitOfWork.VideoComments.GetByIdAsync(commentId, cancellationToken)
            ?? throw new EntityNotFoundException("VideoComment", commentId);
        if (comment.VideoId != videoId)
        {
            throw new EntityNotFoundException("VideoComment", commentId);
        }

        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (comment.UserId != userId && _currentUserService.Role != UserRole.Admin)
        {
            throw new System.UnauthorizedAccessException("You do not have permission to edit this comment");
        }

        comment.Update(request.Comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _unitOfWork.VideoComments.GetByIdWithUserAsync(commentId, cancellationToken)
            ?? throw new EntityNotFoundException("VideoComment", commentId);
        return EngagementMapper.ToComment(updated);
    }
}

public sealed class DeleteCommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCommentService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid videoId, Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await _unitOfWork.VideoComments.GetByIdAsync(commentId, cancellationToken)
            ?? throw new EntityNotFoundException("VideoComment", commentId);
        if (comment.VideoId != videoId)
        {
            throw new EntityNotFoundException("VideoComment", commentId);
        }

        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (comment.UserId != userId && _currentUserService.Role != UserRole.Admin)
        {
            throw new System.UnauthorizedAccessException("You do not have permission to delete this comment");
        }

        await DeleteCommentTreeAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteCommentTreeAsync(VideoComment comment, CancellationToken cancellationToken)
    {
        var children = await _unitOfWork.VideoComments.GetChildrenAsync(comment.Id, cancellationToken);
        foreach (var child in children)
        {
            await DeleteCommentTreeAsync(child, cancellationToken);
        }

        await _unitOfWork.VideoComments.DeleteAsync(comment, cancellationToken);
    }
}

public sealed class ListBookmarksService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListBookmarksService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponseDto<VideoSummaryDto>> Handle(int page, int pageSize, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        var result = await _unitOfWork.Bookmarks.GetPagedByUserIdAsync(userId, Pagination.NormalizePage(page), Pagination.NormalizePageSize(pageSize), cancellationToken);
        return Pagination.Map(result, bookmark => ContentMapper.ToSummary(bookmark.Video));
    }
}

public sealed class SetBookmarkService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public SetBookmarkService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task Handle(Guid videoId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");

        await EngagementGuards.EnsureCanEngageWithVideoAsync(
            _unitOfWork,
            _authorizationService,
            _currentUserService.UserId,
            _currentUserService.Role,
            videoId,
            allowComments: false,
            allowLikes: false,
            allowBookmarks: true,
            cancellationToken);

        var existing = await _unitOfWork.Bookmarks.GetByUserAndVideoAsync(userId, videoId, cancellationToken);
        if (existing is null)
        {
            await _unitOfWork.Bookmarks.AddAsync(Bookmark.Create(userId, videoId), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class RemoveBookmarkService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveBookmarkService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid videoId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        var existing = await _unitOfWork.Bookmarks.GetByUserAndVideoAsync(userId, videoId, cancellationToken);
        if (existing is not null)
        {
            await _unitOfWork.Bookmarks.DeleteAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class ListPlaylistsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListPlaylistsService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponseDto<PlaylistDto>> Handle(ListPlaylistsQuery query, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.Playlists.GetPagedVisibleAsync(
            query.OwnerId,
            _currentUserService.UserId,
            _currentUserService.Role,
            Pagination.NormalizePage(query.Page),
            Pagination.NormalizePageSize(query.PageSize),
            cancellationToken);

        return Pagination.Map(result, EngagementMapper.ToPlaylist);
    }
}

public sealed class ListMyPlaylistsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListMyPlaylistsService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponseDto<PlaylistDto>> Handle(int page, int pageSize, CancellationToken cancellationToken)
    {
        var ownerId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        var result = await _unitOfWork.Playlists.GetPagedByOwnerAsync(ownerId, Pagination.NormalizePage(page), Pagination.NormalizePageSize(pageSize), cancellationToken);
        return Pagination.Map(result, EngagementMapper.ToPlaylist);
    }
}

public sealed class CreatePlaylistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreatePlaylistService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PlaylistDto> Handle(CreatePlaylistRequestDto request, CancellationToken cancellationToken)
    {
        var ownerId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");

        var playlist = Playlist.Create(request.Name, ownerId, request.Description, request.Visibility);
        await _unitOfWork.Playlists.AddAsync(playlist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.Playlists.GetWithDetailsAsync(playlist.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlist.Id);
        return EngagementMapper.ToPlaylist(created);
    }
}

public sealed class GetPlaylistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetPlaylistService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PlaylistDto> Handle(Guid playlistId, CancellationToken cancellationToken)
    {
        var playlist = await _unitOfWork.Playlists.GetWithDetailsAsync(playlistId, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlistId);
        EngagementGuards.EnsureCanViewPlaylist(playlist, _currentUserService.UserId, _currentUserService.Role);
        return EngagementMapper.ToPlaylist(playlist);
    }
}

public sealed class UpdatePlaylistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePlaylistService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PlaylistDto> Handle(Guid playlistId, UpdatePlaylistRequestDto request, CancellationToken cancellationToken)
    {
        var playlist = await _unitOfWork.Playlists.GetByIdAsync(playlistId, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlistId);
        EngagementGuards.EnsureCanManagePlaylist(playlist, _currentUserService.UserId, _currentUserService.Role);
        playlist.Update(request.Name, request.Description, request.Visibility);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _unitOfWork.Playlists.GetWithDetailsAsync(playlistId, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlistId);
        return EngagementMapper.ToPlaylist(updated);
    }
}

public sealed class DeletePlaylistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeletePlaylistService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid playlistId, CancellationToken cancellationToken)
    {
        var playlist = await _unitOfWork.Playlists.GetByIdAsync(playlistId, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlistId);
        EngagementGuards.EnsureCanManagePlaylist(playlist, _currentUserService.UserId, _currentUserService.Role);
        await _unitOfWork.Playlists.DeleteAsync(playlist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetPlaylistVideosService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public GetPlaylistVideosService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<PlaylistVideosPageDto> Handle(Guid playlistId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var playlist = await _unitOfWork.Playlists.GetWithDetailsAsync(playlistId, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlistId);
        EngagementGuards.EnsureCanViewPlaylist(playlist, _currentUserService.UserId, _currentUserService.Role);

        var allPlaylistVideos = await _unitOfWork.PlaylistVideos.GetByPlaylistIdAsync(playlistId, cancellationToken);
        var visibleVideos = new List<VideoSummaryDto>();
        foreach (var playlistVideo in allPlaylistVideos)
        {
            var video = playlistVideo.Video;
            if (video.Status != VideoStatus.Ready)
            {
                continue;
            }

            if (await _authorizationService.CanViewVideoAsync(video.Id, _currentUserService.UserId, _currentUserService.Role, cancellationToken: cancellationToken))
            {
                visibleVideos.Add(ContentMapper.ToSummary(video));
            }
        }

        var normalizedPage = Pagination.NormalizePage(page);
        var normalizedPageSize = Pagination.NormalizePageSize(pageSize);
        var totalCount = visibleVideos.Count;
        var pagedItems = visibleVideos
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArray();
        var totalPages = normalizedPageSize <= 0 ? 0 : (int)Math.Ceiling((double)totalCount / normalizedPageSize);

        return new PlaylistVideosPageDto(
            EngagementMapper.ToPlaylist(playlist),
            new PagedResponseDto<VideoSummaryDto>(
                pagedItems,
                normalizedPage,
                normalizedPageSize,
                totalCount,
                totalPages,
                normalizedPage < totalPages,
                normalizedPage > 1 && totalPages > 0));
    }
}

public sealed class AddPlaylistVideoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public AddPlaylistVideoService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task Handle(Guid playlistId, AddPlaylistVideoRequestDto request, CancellationToken cancellationToken)
    {
        var playlist = await _unitOfWork.Playlists.GetWithDetailsAsync(playlistId, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlistId);
        EngagementGuards.EnsureCanManagePlaylist(playlist, _currentUserService.UserId, _currentUserService.Role);

        await EngagementGuards.EnsureCanViewReadyVideoAsync(
            _unitOfWork,
            _authorizationService,
            _currentUserService.UserId,
            _currentUserService.Role,
            request.VideoId,
            shareToken: null,
            cancellationToken);

        var existing = await _unitOfWork.PlaylistVideos.GetByPlaylistAndVideoAsync(playlistId, request.VideoId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var playlistVideos = (await _unitOfWork.PlaylistVideos.GetByPlaylistIdAsync(playlistId, cancellationToken)).ToList();
        var insertIndex = request.OrderIndex.HasValue
            ? Math.Clamp(request.OrderIndex.Value, 0, playlistVideos.Count)
            : playlistVideos.Count;

        playlistVideos.Insert(insertIndex, PlaylistVideo.Create(playlistId, request.VideoId, insertIndex));
        PlaylistOrdering.Reindex(playlistVideos);

        playlist.IncrementVideoCount();

        await PersistPlaylistVideosAsync(playlistVideos, request.VideoId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistPlaylistVideosAsync(List<PlaylistVideo> playlistVideos, Guid newlyAddedVideoId, CancellationToken cancellationToken)
    {
        foreach (var playlistVideo in playlistVideos)
        {
            if (playlistVideo.VideoId == newlyAddedVideoId)
            {
                var existing = await _unitOfWork.PlaylistVideos.GetByPlaylistAndVideoAsync(playlistVideo.PlaylistId, playlistVideo.VideoId, cancellationToken);
                if (existing is null)
                {
                    await _unitOfWork.PlaylistVideos.AddAsync(playlistVideo, cancellationToken);
                }
                else
                {
                    existing.UpdateOrderIndex(playlistVideo.OrderIndex);
                }
            }
            else
            {
                var existing = await _unitOfWork.PlaylistVideos.GetByPlaylistAndVideoAsync(playlistVideo.PlaylistId, playlistVideo.VideoId, cancellationToken);
                existing?.UpdateOrderIndex(playlistVideo.OrderIndex);
            }
        }
    }
}

public sealed class RemovePlaylistVideoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemovePlaylistVideoService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid playlistId, Guid videoId, CancellationToken cancellationToken)
    {
        var playlist = await _unitOfWork.Playlists.GetByIdAsync(playlistId, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlistId);
        EngagementGuards.EnsureCanManagePlaylist(playlist, _currentUserService.UserId, _currentUserService.Role);

        var existing = await _unitOfWork.PlaylistVideos.GetByPlaylistAndVideoAsync(playlistId, videoId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        await _unitOfWork.PlaylistVideos.DeleteAsync(existing, cancellationToken);
        playlist.DecrementVideoCount();

        var remaining = (await _unitOfWork.PlaylistVideos.GetByPlaylistIdAsync(playlistId, cancellationToken))
            .Where(playlistVideo => playlistVideo.VideoId != videoId)
            .ToList();
        PlaylistOrdering.Reindex(remaining);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ReorderPlaylistVideosService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ReorderPlaylistVideosService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid playlistId, ReorderPlaylistVideosRequestDto request, CancellationToken cancellationToken)
    {
        var playlist = await _unitOfWork.Playlists.GetByIdAsync(playlistId, cancellationToken)
            ?? throw new EntityNotFoundException("Playlist", playlistId);
        EngagementGuards.EnsureCanManagePlaylist(playlist, _currentUserService.UserId, _currentUserService.Role);

        var playlistVideos = (await _unitOfWork.PlaylistVideos.GetByPlaylistIdAsync(playlistId, cancellationToken)).ToList();
        var currentIds = playlistVideos.Select(playlistVideo => playlistVideo.VideoId).OrderBy(id => id).ToArray();
        var requestedIds = request.VideoIds.Distinct().OrderBy(id => id).ToArray();
        if (!currentIds.SequenceEqual(requestedIds))
        {
            throw new InvalidOperationException("Reorder request must contain the complete current set of playlist video IDs");
        }

        var lookup = playlistVideos.ToDictionary(playlistVideo => playlistVideo.VideoId);
        for (var index = 0; index < request.VideoIds.Count; index++)
        {
            lookup[request.VideoIds[index]].UpdateOrderIndex(index);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListNotificationsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListNotificationsService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponseDto<NotificationDto>> Handle(ListNotificationsQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        var result = await _unitOfWork.Notifications.GetPagedByUserIdAsync(
            userId,
            query.IsRead,
            Pagination.NormalizePage(query.Page),
            Pagination.NormalizePageSize(query.PageSize),
            cancellationToken);
        return Pagination.Map(result, EngagementMapper.ToNotification);
    }
}

public sealed class GetUnreadNotificationCountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationCountService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UnreadNotificationCountDto> Handle(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        return new UnreadNotificationCountDto(await _unitOfWork.Notifications.GetUnreadCountAsync(userId, cancellationToken));
    }
}

public sealed class MarkNotificationReadStateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MarkNotificationReadStateService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid notificationId, bool isRead, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        var notification = await _unitOfWork.Notifications.GetByIdForUserAsync(notificationId, userId, cancellationToken)
            ?? throw new EntityNotFoundException("Notification", notificationId);

        if (isRead)
        {
            notification.MarkAsRead();
        }
        else
        {
            notification.MarkAsUnread();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class MarkAllNotificationsReadService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MarkAllNotificationsReadService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

internal static class EngagementGuards
{
    public static async Task<Video> EnsureCanEngageWithVideoAsync(
        IUnitOfWork unitOfWork,
        IAuthorizationService authorizationService,
        Guid? currentUserId,
        UserRole? currentUserRole,
        Guid videoId,
        bool allowComments,
        bool allowLikes,
        bool allowBookmarks,
        CancellationToken cancellationToken)
    {
        var video = await EnsureCanViewReadyVideoAsync(unitOfWork, authorizationService, currentUserId, currentUserRole, videoId, null, cancellationToken);

        if (allowComments && !video.AllowComments)
        {
            throw new InvalidOperationException("Comments are disabled for this video");
        }

        if (allowLikes && !video.AllowLikes)
        {
            throw new InvalidOperationException("Reactions are disabled for this video");
        }

        if (allowBookmarks && !video.AllowBookmarks)
        {
            throw new InvalidOperationException("Bookmarks are disabled for this video");
        }

        return video;
    }

    public static async Task<Video> EnsureCanViewReadyVideoAsync(
        IUnitOfWork unitOfWork,
        IAuthorizationService authorizationService,
        Guid? currentUserId,
        UserRole? currentUserRole,
        Guid videoId,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        var video = await unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        if (video.Status != VideoStatus.Ready)
        {
            throw new InvalidOperationException("Video is not ready");
        }

        if (!await authorizationService.CanViewVideoAsync(videoId, currentUserId, currentUserRole, shareToken, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have access to this video");
        }

        return video;
    }

    public static void EnsureCanViewPlaylist(Playlist playlist, Guid? currentUserId, UserRole? currentUserRole)
    {
        if (currentUserRole == UserRole.Admin)
        {
            return;
        }

        if (playlist.Visibility == PlaylistVisibility.Public)
        {
            return;
        }

        if (!currentUserId.HasValue || playlist.OwnerId != currentUserId.Value)
        {
            throw new System.UnauthorizedAccessException("You do not have access to this playlist");
        }
    }

    public static void EnsureCanManagePlaylist(Playlist playlist, Guid? currentUserId, UserRole? currentUserRole)
    {
        if (currentUserRole == UserRole.Admin)
        {
            return;
        }

        if (!currentUserId.HasValue || playlist.OwnerId != currentUserId.Value)
        {
            throw new System.UnauthorizedAccessException("You do not have permission to manage this playlist");
        }
    }
}

internal static class EngagementMapper
{
    public static CommentDto ToComment(VideoComment comment)
    {
        return new CommentDto(
            comment.Id,
            comment.VideoId,
            comment.UserId,
            comment.User?.Name ?? string.Empty,
            comment.ParentCommentId,
            comment.Comment,
            comment.IsEdited,
            comment.CreatedAt,
            comment.UpdatedAt);
    }

    public static PlaylistDto ToPlaylist(Playlist playlist)
    {
        return new PlaylistDto(
            playlist.Id,
            playlist.Name,
            playlist.Description,
            playlist.OwnerId,
            playlist.Owner?.Name ?? string.Empty,
            playlist.Visibility,
            playlist.VideoCount,
            playlist.CreatedAt,
            playlist.UpdatedAt);
    }

    public static NotificationDto ToNotification(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.VideoId,
            notification.NotificationType,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt);
    }
}

internal static class PlaylistOrdering
{
    public static void Reindex(List<PlaylistVideo> playlistVideos)
    {
        for (var index = 0; index < playlistVideos.Count; index++)
        {
            playlistVideos[index].UpdateOrderIndex(index);
        }
    }
}
