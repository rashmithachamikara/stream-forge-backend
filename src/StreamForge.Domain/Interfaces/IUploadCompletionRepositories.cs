using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

public interface IStorageProviderRepository : IRepository<StorageProvider>
{
    Task<StorageProvider?> GetDefaultByTypeAsync(StorageProviderType type, CancellationToken cancellationToken = default);
}

public interface IVideoVersionRepository : IRepository<VideoVersion>
{
}

public interface IVideoFileRepository : IRepository<VideoFile>
{
}

public interface IVideoTagRepository
{
    Task AddAsync(VideoTag videoTag, CancellationToken cancellationToken = default);
}
