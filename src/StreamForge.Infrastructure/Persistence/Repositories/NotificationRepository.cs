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

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(notification => notification.UserId == userId && !notification.IsRead).OrderByDescending(notification => notification.CreatedAt).ToListAsync(cancellationToken);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await DbSet.Where(notification => notification.UserId == userId && !notification.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        DbSet.CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken);
}
