using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class PlaylistRepository : BaseRepository<Playlist>, IPlaylistRepository
{
    public PlaylistRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Playlist?> GetWithDetailsAsync(Guid playlistId, CancellationToken cancellationToken = default) =>
        _dbSet
            .Include(playlist => playlist.Owner)
            .Include(playlist => playlist.PlaylistVideos)
                .ThenInclude(playlistVideo => playlistVideo.Video)
                    .ThenInclude(video => video.Uploader)
            .Include(playlist => playlist.PlaylistVideos)
                .ThenInclude(playlistVideo => playlistVideo.Video)
                    .ThenInclude(video => video.Category)
            .Include(playlist => playlist.PlaylistVideos)
                .ThenInclude(playlistVideo => playlistVideo.Video)
                    .ThenInclude(video => video.VideoTags)
                        .ThenInclude(videoTag => videoTag.Tag)
            .FirstOrDefaultAsync(playlist => playlist.Id == playlistId, cancellationToken);

    public async Task<PagedQueryResult<Playlist>> GetPagedVisibleAsync(
        Guid? ownerId,
        Guid? currentUserId,
        UserRole? currentUserRole,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(playlist => playlist.Owner)
            .AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(playlist => playlist.OwnerId == ownerId.Value);
        }

        if (currentUserRole == UserRole.Admin)
        {
            query = query.OrderByDescending(playlist => playlist.CreatedAt).ThenByDescending(playlist => playlist.Id);
        }
        else if (currentUserId.HasValue)
        {
            query = query
                .Where(playlist => playlist.Visibility == PlaylistVisibility.Public || playlist.OwnerId == currentUserId.Value)
                .OrderByDescending(playlist => playlist.CreatedAt)
                .ThenByDescending(playlist => playlist.Id);
        }
        else
        {
            query = query
                .Where(playlist => playlist.Visibility == PlaylistVisibility.Public)
                .OrderByDescending(playlist => playlist.CreatedAt)
                .ThenByDescending(playlist => playlist.Id);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<Playlist>(items, totalCount, page, pageSize);
    }

    public async Task<PagedQueryResult<Playlist>> GetPagedByOwnerAsync(
        Guid ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(playlist => playlist.Owner)
            .Where(playlist => playlist.OwnerId == ownerId)
            .OrderByDescending(playlist => playlist.CreatedAt)
            .ThenByDescending(playlist => playlist.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<Playlist>(items, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<Playlist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(playlist => playlist.OwnerId == userId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Playlist>> GetPublicPlaylistsAsync(CancellationToken cancellationToken = default) =>
        await _dbSet.Where(playlist => playlist.Visibility == PlaylistVisibility.Public).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Playlist>> GetPlaylistsContainingVideoAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(playlist => playlist.PlaylistVideos.Any(playlistVideo => playlistVideo.VideoId == videoId)).ToListAsync(cancellationToken);
}
