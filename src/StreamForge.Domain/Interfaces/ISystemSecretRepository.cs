using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

public interface ISystemSecretRepository : IRepository<SystemSecret>
{
    Task<SystemSecret?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemSecret>> GetByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);
}
