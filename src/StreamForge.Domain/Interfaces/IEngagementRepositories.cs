using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

public sealed record ReactionSummaryResult(
    int LikeCount,
    int DislikeCount,
    ReactionType? CurrentUserReaction);

public interface IVideoReactionRepository : IRepository<VideoReaction>
{
    Task<VideoReaction?> GetByUserAndVideoAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default);

    Task<ReactionSummaryResult> GetSummaryAsync(Guid videoId, Guid? currentUserId, CancellationToken cancellationToken = default);
}

public interface IVideoCommentRepository : IRepository<VideoComment>
{
    Task<VideoComment?> GetByIdWithUserAsync(Guid commentId, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<VideoComment>> GetPagedByVideoIdAsync(
        Guid videoId,
        Guid? parentCommentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoComment>> GetChildrenAsync(Guid parentCommentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetReplyCountsAsync(
        IReadOnlyCollection<Guid> commentIds,
        CancellationToken cancellationToken = default);
}

public interface IBookmarkRepository : IRepository<Bookmark>
{
    Task<Bookmark?> GetByIdForUserAsync(Guid bookmarkId, Guid userId, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<Bookmark>> GetPagedByVideoIdAsync(
        Guid userId,
        Guid videoId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<Bookmark>> GetPagedByUserIdAsync(
        Guid userId,
        Guid? videoId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IPlaylistVideoRepository
{
    Task<PlaylistVideo?> GetByPlaylistAndVideoAsync(Guid playlistId, Guid videoId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaylistVideo>> GetByPlaylistIdAsync(Guid playlistId, CancellationToken cancellationToken = default);

    Task AddAsync(PlaylistVideo playlistVideo, CancellationToken cancellationToken = default);

    Task DeleteAsync(PlaylistVideo playlistVideo, CancellationToken cancellationToken = default);
}
