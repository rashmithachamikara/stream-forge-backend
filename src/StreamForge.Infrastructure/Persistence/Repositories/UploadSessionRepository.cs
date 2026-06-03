using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for UploadSession
/// </summary>
public class UploadSessionRepository : BaseRepository<UploadSession>, IUploadSessionRepository
{
    public UploadSessionRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<PagedQueryResult<UploadSession>> GetByUserIdPagedAsync(
        Guid userId,
        UploadSessionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(session => session.Video)
            .Where(session => session.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(session => session.Status == status.Value);
        }

        query = query.OrderByDescending(session => session.CreatedAt).ThenByDescending(session => session.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<UploadSession>(items, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<UploadSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UploadSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId && 
                        (s.Status == UploadSessionStatus.Created || 
                         s.Status == UploadSessionStatus.Active))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UploadSession>> GetByStatusAsync(UploadSessionStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UploadSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .Where(s => s.ExpiresAt < now && 
                        s.Status != UploadSessionStatus.Completed && 
                        s.Status != UploadSessionStatus.Expired)
            .ToListAsync(cancellationToken);
    }

    public async Task<UploadSession?> GetWithPartsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await DbContext.UploadSessions
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }
}
