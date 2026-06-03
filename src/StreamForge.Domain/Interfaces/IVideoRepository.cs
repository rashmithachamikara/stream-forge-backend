using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for Video entity
/// </summary>
public interface IVideoRepository : IRepository<Video>
{
    Task<Video?> GetWithDetailsAsync(Guid videoId, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<Video>> SearchVisibleAsync(
        string? searchTerm,
        Guid? categoryId,
        Guid? tagId,
        Guid? uploaderId,
        VideoStatus? status,
        VideoVisibility? visibility,
        Guid? currentUserId,
        UserRole? currentUserRole,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<Video>> GetUserLibraryAsync(
        Guid userId,
        VideoStatus? status,
        VideoVisibility? visibility,
        string? searchTerm,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets videos by user ID
    /// </summary>
    Task<IEnumerable<Video>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets videos by category ID
    /// </summary>
    Task<IEnumerable<Video>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets videos by tag ID
    /// </summary>
    Task<IEnumerable<Video>> GetByTagIdAsync(Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets videos by visibility
    /// </summary>
    Task<IEnumerable<Video>> GetByVisibilityAsync(VideoVisibility visibility, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches videos by title or description
    /// </summary>
    Task<IEnumerable<Video>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets most viewed videos
    /// </summary>
    Task<IEnumerable<Video>> GetMostViewedAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent videos
    /// </summary>
    Task<IEnumerable<Video>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
