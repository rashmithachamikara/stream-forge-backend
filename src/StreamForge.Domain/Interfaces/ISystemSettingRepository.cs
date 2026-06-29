using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

public interface ISystemSettingRepository : IRepository<SystemSetting>
{
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemSetting>> GetByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);
}
