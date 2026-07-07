using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Notification?> GetByIdForUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        _dbSet.FirstOrDefaultAsync(notification => notification.Id == notificationId && notification.UserId == userId, cancellationToken);

    public async Task<PagedQueryResult<Notification>> GetPagedByUserIdAsync(
        Guid userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(notification => notification.IsRead == isRead.Value);
        }

        query = query.OrderByDescending(notification => notification.CreatedAt).ThenByDescending(notification => notification.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<Notification>(items, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(notification => notification.UserId == userId && !notification.IsRead).OrderByDescending(notification => notification.CreatedAt).ToListAsync(cancellationToken);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _dbSet.Where(notification => notification.UserId == userId && !notification.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }
    }

    public async Task DeleteReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _dbSet
            .Where(notification => notification.UserId == userId && notification.IsRead)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(notifications);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbSet.CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken);
}
