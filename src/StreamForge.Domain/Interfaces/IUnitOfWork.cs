using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface for transaction management
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// User repository
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Video repository
    /// </summary>
    IVideoRepository Videos { get; }

    /// <summary>
    /// Category repository
    /// </summary>
    ICategoryRepository Categories { get; }

    /// <summary>
    /// Tag repository
    /// </summary>
    ITagRepository Tags { get; }

    /// <summary>
    /// Playlist repository
    /// </summary>
    IPlaylistRepository Playlists { get; }

    /// <summary>
    /// Notification repository
    /// </summary>
    INotificationRepository Notifications { get; }

    /// <summary>
    /// Analytics repository
    /// </summary>
    IAnalyticsEventRepository AnalyticsEvents { get; }

    /// <summary>
    /// Saves all changes made in this unit of work
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
