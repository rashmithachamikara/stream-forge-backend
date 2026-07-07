using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class SystemSettingRepository : BaseRepository<SystemSetting>, ISystemSettingRepository
{
    public SystemSettingRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalizedKey = key.Trim();
        return _dbContext.SystemSettings.FirstOrDefaultAsync(setting => setting.Key == normalizedKey, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemSetting>> GetByKeysAsync(
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

        return await _dbContext.SystemSettings
            .Where(setting => normalizedKeys.Contains(setting.Key))
            .ToListAsync(cancellationToken);
    }
}
