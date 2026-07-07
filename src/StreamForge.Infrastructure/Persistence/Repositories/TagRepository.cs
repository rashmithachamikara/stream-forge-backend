using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class TagRepository : BaseRepository<Tag>, ITagRepository
{
    public TagRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<PagedQueryResult<Tag>> SearchPagedAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim().ToLowerInvariant()}%";
            query = query.Where(tag => EF.Functions.ILike(tag.Name, pattern));
        }

        query = query
            .OrderByDescending(tag => tag.UsageCount)
            .ThenBy(tag => tag.Name)
            .ThenBy(tag => tag.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<Tag>(items, totalCount, page, pageSize);
    }

    public Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _dbSet.FirstOrDefaultAsync(tag => tag.Name == name.Trim().ToLowerInvariant(), cancellationToken);

    public async Task<IEnumerable<Tag>> SearchByNameAsync(string namePattern, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(tag => tag.Name.Contains(namePattern.Trim().ToLowerInvariant())).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Tag>> GetMostUsedAsync(int count, CancellationToken cancellationToken = default) =>
        await _dbSet.OrderByDescending(tag => tag.UsageCount).Take(count).ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeTagId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var query = _dbSet.Where(tag => tag.Name == normalizedName);
        if (excludeTagId.HasValue)
        {
            query = query.Where(tag => tag.Id != excludeTagId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsInUseAsync(Guid tagId, CancellationToken cancellationToken = default) =>
        _dbContext.VideoTags.AnyAsync(videoTag => videoTag.TagId == tagId, cancellationToken);
}
