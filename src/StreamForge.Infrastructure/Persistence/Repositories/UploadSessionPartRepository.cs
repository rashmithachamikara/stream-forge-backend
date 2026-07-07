using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for UploadSessionPart
/// </summary>
public class UploadSessionPartRepository : BaseRepository<UploadSessionPart>, IUploadSessionPartRepository
{
    public UploadSessionPartRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<UploadSessionPart>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UploadSessionId == sessionId)
            .OrderBy(p => p.PartNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<UploadSessionPart?> GetBySessionAndPartAsync(Guid sessionId, int partNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.UploadSessionId == sessionId && p.PartNumber == partNumber, cancellationToken);
    }

    public async Task<IEnumerable<UploadSessionPart>> GetCompletePartsBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UploadSessionId == sessionId && p.IsComplete)
            .OrderBy(p => p.PartNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCompletePartCountAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UploadSessionId == sessionId && p.IsComplete)
            .CountAsync(cancellationToken);
    }

    public async Task DeleteBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var parts = await _dbSet
            .Where(p => p.UploadSessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (parts.Any())
        {
            _dbSet.RemoveRange(parts);
        }
    }
}
