using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class VideoReactionRepository : BaseRepository<VideoReaction>, IVideoReactionRepository
{
    public VideoReactionRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<VideoReaction?> GetByUserAndVideoAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(reaction => reaction.UserId == userId && reaction.VideoId == videoId, cancellationToken);

    public async Task<ReactionSummaryResult> GetSummaryAsync(Guid videoId, Guid? currentUserId, CancellationToken cancellationToken = default)
    {
        var reactions = await DbSet
            .AsNoTracking()
            .Where(reaction => reaction.VideoId == videoId)
            .Select(reaction => new
            {
                reaction.UserId,
                reaction.ReactionType
            })
            .ToListAsync(cancellationToken);

        var likeCount = reactions.Count(reaction => reaction.ReactionType == ReactionType.Like);
        var dislikeCount = reactions.Count(reaction => reaction.ReactionType == ReactionType.Dislike);
        var currentReaction = currentUserId.HasValue
            ? reactions
                .Where(reaction => reaction.UserId == currentUserId.Value)
                .Select(reaction => (ReactionType?)reaction.ReactionType)
                .FirstOrDefault()
            : null;

        return new ReactionSummaryResult(likeCount, dislikeCount, currentReaction);
    }
}

public sealed class VideoCommentRepository : BaseRepository<VideoComment>, IVideoCommentRepository
{
    public VideoCommentRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<VideoComment?> GetByIdWithUserAsync(Guid commentId, CancellationToken cancellationToken = default) =>
        DbSet
            .AsNoTracking()
            .Include(comment => comment.User)
            .FirstOrDefaultAsync(comment => comment.Id == commentId, cancellationToken);

    public async Task<PagedQueryResult<VideoComment>> GetPagedByVideoIdAsync(
        Guid videoId,
        Guid? parentCommentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(comment => comment.User)
            .Where(comment => comment.VideoId == videoId && comment.ParentCommentId == parentCommentId)
            .OrderByDescending(comment => comment.CreatedAt)
            .ThenByDescending(comment => comment.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<VideoComment>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<VideoComment>> GetChildrenAsync(Guid parentCommentId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(comment => comment.ParentCommentId == parentCommentId)
            .OrderByDescending(comment => comment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetReplyCountsAsync(
        IReadOnlyCollection<Guid> commentIds,
        CancellationToken cancellationToken = default)
    {
        if (commentIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await DbSet
            .AsNoTracking()
            .Where(comment => comment.ParentCommentId.HasValue && commentIds.Contains(comment.ParentCommentId.Value))
            .GroupBy(comment => comment.ParentCommentId!.Value)
            .Select(group => new { CommentId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.CommentId, item => item.Count, cancellationToken);
    }
}

public sealed class BookmarkRepository : BaseRepository<Bookmark>, IBookmarkRepository
{
    public BookmarkRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Bookmark?> GetByIdForUserAsync(Guid bookmarkId, Guid userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(bookmark => bookmark.Video)
                .ThenInclude(video => video.Uploader)
            .Include(bookmark => bookmark.Video)
                .ThenInclude(video => video.Category)
            .Include(bookmark => bookmark.Video)
                .ThenInclude(video => video.VideoTags)
                    .ThenInclude(videoTag => videoTag.Tag)
            .FirstOrDefaultAsync(bookmark => bookmark.Id == bookmarkId && bookmark.UserId == userId, cancellationToken);

    public async Task<PagedQueryResult<Bookmark>> GetPagedByVideoIdAsync(
        Guid userId,
        Guid videoId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(bookmark => bookmark.UserId == userId && bookmark.VideoId == videoId)
            .OrderBy(bookmark => bookmark.TimestampSeconds)
            .ThenByDescending(bookmark => bookmark.CreatedAt)
            .ThenByDescending(bookmark => bookmark.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<Bookmark>(items, totalCount, page, pageSize);
    }

    public async Task<PagedQueryResult<Bookmark>> GetPagedByUserIdAsync(
        Guid userId,
        Guid? videoId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(bookmark => bookmark.Video)
                .ThenInclude(video => video.Uploader)
            .Include(bookmark => bookmark.Video)
                .ThenInclude(video => video.Category)
            .Include(bookmark => bookmark.Video)
                .ThenInclude(video => video.VideoTags)
                    .ThenInclude(videoTag => videoTag.Tag)
            .Where(bookmark => bookmark.UserId == userId);

        if (videoId.HasValue)
        {
            query = query.Where(bookmark => bookmark.VideoId == videoId.Value);
        }

        query = query
            .OrderByDescending(bookmark => bookmark.CreatedAt)
            .ThenByDescending(bookmark => bookmark.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<Bookmark>(items, totalCount, page, pageSize);
    }
}

public sealed class PlaylistVideoRepository : IPlaylistVideoRepository
{
    private readonly StreamForgeDbContext _dbContext;

    public PlaylistVideoRepository(StreamForgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PlaylistVideo?> GetByPlaylistAndVideoAsync(Guid playlistId, Guid videoId, CancellationToken cancellationToken = default) =>
        _dbContext.PlaylistVideos
            .Include(playlistVideo => playlistVideo.Video)
                .ThenInclude(video => video.Uploader)
            .Include(playlistVideo => playlistVideo.Video)
                .ThenInclude(video => video.Category)
            .Include(playlistVideo => playlistVideo.Video)
                .ThenInclude(video => video.VideoTags)
                    .ThenInclude(videoTag => videoTag.Tag)
            .FirstOrDefaultAsync(playlistVideo => playlistVideo.PlaylistId == playlistId && playlistVideo.VideoId == videoId, cancellationToken);

    public async Task<IReadOnlyList<PlaylistVideo>> GetByPlaylistIdAsync(Guid playlistId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlaylistVideos
            .Include(playlistVideo => playlistVideo.Video)
                .ThenInclude(video => video.Uploader)
            .Include(playlistVideo => playlistVideo.Video)
                .ThenInclude(video => video.Category)
            .Include(playlistVideo => playlistVideo.Video)
                .ThenInclude(video => video.VideoTags)
                    .ThenInclude(videoTag => videoTag.Tag)
            .Where(playlistVideo => playlistVideo.PlaylistId == playlistId)
            .OrderBy(playlistVideo => playlistVideo.OrderIndex)
            .ThenBy(playlistVideo => playlistVideo.VideoId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlaylistVideo playlistVideo, CancellationToken cancellationToken = default)
    {
        await _dbContext.PlaylistVideos.AddAsync(playlistVideo, cancellationToken);
    }

    public Task DeleteAsync(PlaylistVideo playlistVideo, CancellationToken cancellationToken = default)
    {
        _dbContext.PlaylistVideos.Remove(playlistVideo);
        return Task.CompletedTask;
    }
}
