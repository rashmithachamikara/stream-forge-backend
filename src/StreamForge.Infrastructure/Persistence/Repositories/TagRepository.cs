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

    public Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(tag => tag.Name == name.Trim().ToLowerInvariant(), cancellationToken);

    public async Task<IEnumerable<Tag>> SearchByNameAsync(string namePattern, CancellationToken cancellationToken = default) =>
        await DbSet.Where(tag => tag.Name.Contains(namePattern.Trim().ToLowerInvariant())).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Tag>> GetMostUsedAsync(int count, CancellationToken cancellationToken = default) =>
        await DbSet.OrderByDescending(tag => tag.UsageCount).Take(count).ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(tag => tag.Name == name.Trim().ToLowerInvariant(), cancellationToken);
}
