using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for Notification entity
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    Task<Notification?> GetByIdForUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<Notification>> GetPagedByUserIdAsync(
        Guid userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications by user ID
    /// </summary>
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notifications by user ID
    /// </summary>
    Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification count for user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
