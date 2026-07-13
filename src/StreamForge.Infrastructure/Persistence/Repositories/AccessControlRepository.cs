using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class AccessControlRepository : BaseRepository<AccessControl>, IAccessControlRepository
{
    public AccessControlRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<PagedQueryResult<AccessControl>> GetByVideoIdPagedAsync(
        Guid videoId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(accessControl => accessControl.User)
            .Where(accessControl => accessControl.VideoId == videoId);

        if (isActive.HasValue)
        {
            query = query.Where(accessControl => accessControl.IsActive == isActive.Value);
        }

        query = query
            .OrderByDescending(accessControl => accessControl.CreatedAt)
            .ThenByDescending(accessControl => accessControl.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<AccessControl>(items, totalCount, page, pageSize);
    }

    public async Task<AccessControl?> GetByIdWithUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(accessControl => accessControl.User)
            .FirstOrDefaultAsync(accessControl => accessControl.Id == id, cancellationToken);
    }
}
