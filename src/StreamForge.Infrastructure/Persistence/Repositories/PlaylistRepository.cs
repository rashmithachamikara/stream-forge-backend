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

    public async Task<IEnumerable<Playlist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(playlist => playlist.OwnerId == userId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Playlist>> GetPublicPlaylistsAsync(CancellationToken cancellationToken = default) =>
        await DbSet.Where(playlist => playlist.Visibility == PlaylistVisibility.Public).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Playlist>> GetPlaylistsContainingVideoAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(playlist => playlist.PlaylistVideos.Any(playlistVideo => playlistVideo.VideoId == videoId)).ToListAsync(cancellationToken);
}
