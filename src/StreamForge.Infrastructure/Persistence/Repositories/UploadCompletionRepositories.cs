using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class StorageProviderRepository : BaseRepository<StorageProvider>, IStorageProviderRepository
{
    public StorageProviderRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<StorageProvider?> GetDefaultByTypeAsync(StorageProviderType type, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(provider => provider.Type == type && provider.IsDefault && provider.IsActive, cancellationToken);
}

public sealed class VideoVersionRepository : BaseRepository<VideoVersion>, IVideoVersionRepository
{
    public VideoVersionRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<VideoVersion>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(version => version.VideoId == videoId).ToListAsync(cancellationToken);
}

public sealed class VideoFileRepository : BaseRepository<VideoFile>, IVideoFileRepository
{
    public VideoFileRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<VideoFile?> GetOriginalByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(file => file.VideoVersion)
            .Where(file => file.VideoVersion.VideoId == videoId && file.VideoVersion.Resolution == "original")
            .OrderBy(file => file.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class VideoTagRepository : IVideoTagRepository
{
    private readonly StreamForgeDbContext _dbContext;

    public VideoTagRepository(StreamForgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(VideoTag videoTag, CancellationToken cancellationToken = default)
    {
        await _dbContext.VideoTags.AddAsync(videoTag, cancellationToken);
    }
}

public sealed class VideoThumbnailRepository : BaseRepository<VideoThumbnail>, IVideoThumbnailRepository
{
    public VideoThumbnailRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<VideoThumbnail?> GetDefaultByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(thumbnail => thumbnail.VideoId == videoId && thumbnail.IsDefault, cancellationToken);
}

public sealed class VideoProcessingJobRepository : BaseRepository<VideoProcessingJob>, IVideoProcessingJobRepository
{
    public VideoProcessingJobRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }
}
