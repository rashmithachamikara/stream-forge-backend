using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

public interface IStorageProviderRepository : IRepository<StorageProvider>
{
    Task<StorageProvider?> GetDefaultByTypeAsync(StorageProviderType type, CancellationToken cancellationToken = default);
}

public interface IVideoVersionRepository : IRepository<VideoVersion>
{
    Task<IEnumerable<VideoVersion>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);
}

public interface IVideoFileRepository : IRepository<VideoFile>
{
    Task<VideoFile?> GetOriginalByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);
}

public interface IVideoTagRepository
{
    Task AddAsync(VideoTag videoTag, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTag>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);

    Task DeleteByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);
}

public interface IVideoThumbnailRepository : IRepository<VideoThumbnail>
{
    Task<VideoThumbnail?> GetDefaultByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);
}

public interface IVideoProcessingJobRepository : IRepository<VideoProcessingJob>
{
    Task<VideoProcessingJob?> GetLatestByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);
    Task<VideoProcessingJob?> GetWithVideoAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VideoProcessingJob>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VideoProcessingJob>> GetByStatusesAsync(
        CancellationToken cancellationToken = default,
        params ProcessingJobStatus[] statuses);
    Task<PagedQueryResult<VideoProcessingJob>> QueryAdminAsync(
        AdminVideoProcessingJobsQuery query,
        CancellationToken cancellationToken = default);
}
