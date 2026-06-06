using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for Playlist entity
/// </summary>
public interface IPlaylistRepository : IRepository<Playlist>
{
    Task<Playlist?> GetWithDetailsAsync(Guid playlistId, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<Playlist>> GetPagedVisibleAsync(
        Guid? ownerId,
        Guid? currentUserId,
        UserRole? currentUserRole,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<Playlist>> GetPagedByOwnerAsync(
        Guid ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

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
