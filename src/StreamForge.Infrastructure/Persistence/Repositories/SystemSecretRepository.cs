using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class SystemSecretRepository : BaseRepository<SystemSecret>, ISystemSecretRepository
{
    public SystemSecretRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<SystemSecret?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalizedKey = key.Trim();
        return DbContext.SystemSecrets.FirstOrDefaultAsync(setting => setting.Key == normalizedKey, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemSecret>> GetByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        var normalizedKeys = keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedKeys.Length == 0)
        {
            return [];
        }

        return await DbContext.SystemSecrets
            .Where(setting => normalizedKeys.Contains(setting.Key))
            .ToListAsync(cancellationToken);
    }
}
