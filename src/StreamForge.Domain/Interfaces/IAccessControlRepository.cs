using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

public interface IAccessControlRepository : IRepository<AccessControl>
{
    Task<PagedQueryResult<AccessControl>> GetByVideoIdPagedAsync(
        Guid videoId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
