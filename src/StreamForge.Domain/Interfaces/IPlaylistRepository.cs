using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for Playlist entity
/// </summary>
public interface IPlaylistRepository : IRepository<Playlist>
{
    /// <summary>
    /// Gets playlists by user ID
    /// </summary>
    Task<IEnumerable<Playlist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets public playlists
    /// </summary>
    Task<IEnumerable<Playlist>> GetPublicPlaylistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets playlists containing a specific video
    /// </summary>
    Task<IEnumerable<Playlist>> GetPlaylistsContainingVideoAsync(Guid videoId, CancellationToken cancellationToken = default);
}
