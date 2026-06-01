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
