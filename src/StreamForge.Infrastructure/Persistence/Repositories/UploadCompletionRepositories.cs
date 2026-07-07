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
        _dbSet.FirstOrDefaultAsync(provider => provider.Type == type && provider.IsDefault && provider.IsActive, cancellationToken);
}

public sealed class VideoVersionRepository : BaseRepository<VideoVersion>, IVideoVersionRepository
{
    public VideoVersionRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<VideoVersion>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(version => version.VideoId == videoId).ToListAsync(cancellationToken);
}

public sealed class VideoFileRepository : BaseRepository<VideoFile>, IVideoFileRepository
{
    public VideoFileRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<VideoFile?> GetOriginalByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        _dbSet
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

    public async Task<IReadOnlyList<VideoTag>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VideoTags
            .Include(videoTag => videoTag.Tag)
            .Where(videoTag => videoTag.VideoId == videoId)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        var videoTags = await _dbContext.VideoTags
            .Where(videoTag => videoTag.VideoId == videoId)
            .ToListAsync(cancellationToken);

        _dbContext.VideoTags.RemoveRange(videoTags);
    }
}

public sealed class VideoThumbnailRepository : BaseRepository<VideoThumbnail>, IVideoThumbnailRepository
{
    public VideoThumbnailRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<VideoThumbnail?> GetDefaultByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        _dbSet.FirstOrDefaultAsync(thumbnail => thumbnail.VideoId == videoId && thumbnail.IsDefault, cancellationToken);
}

public sealed class VideoProcessingJobRepository : BaseRepository<VideoProcessingJob>, IVideoProcessingJobRepository
{
    public VideoProcessingJobRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<VideoProcessingJob?> GetLatestByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        _dbSet
            .Include(job => job.Video)
            .Where(job => job.VideoId == videoId)
            .OrderByDescending(job => job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<VideoProcessingJob?> GetWithVideoAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        _dbSet
            .Include(job => job.Video)
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

    public async Task<IReadOnlyList<VideoProcessingJob>> GetAllOrderedAsync(CancellationToken cancellationToken = default) =>
        await _dbSet
            .AsNoTracking()
            .Include(job => job.Video)
            .OrderByDescending(job => job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VideoProcessingJob>> GetByStatusesAsync(
        CancellationToken cancellationToken = default,
        params ProcessingJobStatus[] statuses)
    {
        if (statuses is null || statuses.Length == 0)
        {
            return [];
        }

        return await _dbSet
            .AsNoTracking()
            .Include(job => job.Video)
            .Where(job => statuses.Contains(job.Status))
            .OrderByDescending(job => job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedQueryResult<VideoProcessingJob>> QueryAdminAsync(
        AdminVideoProcessingJobsQuery query,
        CancellationToken cancellationToken = default)
    {
        var itemsQuery = _dbSet
            .AsNoTracking()
            .Include(job => job.Video)
            .AsQueryable();

        if (query.Status.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.Status == query.Status.Value);
        }

        if (query.VideoId.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.VideoId == query.VideoId.Value);
        }

        if (query.UploaderUserId.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.Video.UploaderId == query.UploaderUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var normalizedSearch = query.Search.Trim();
            var pattern = $"%{normalizedSearch}%";
            var hasJobId = Guid.TryParse(normalizedSearch, out var parsedJobId);

            itemsQuery = itemsQuery.Where(job =>
                EF.Functions.ILike(job.Video.Title, pattern) ||
                (hasJobId && job.Id == parsedJobId));
        }

        if (query.CreatedFrom.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.CreatedAt <= query.CreatedTo.Value);
        }

        if (query.StartedFrom.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.StartedAt.HasValue && job.StartedAt.Value >= query.StartedFrom.Value);
        }

        if (query.StartedTo.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.StartedAt.HasValue && job.StartedAt.Value <= query.StartedTo.Value);
        }

        if (query.CompletedFrom.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.CompletedAt.HasValue && job.CompletedAt.Value >= query.CompletedFrom.Value);
        }

        if (query.CompletedTo.HasValue)
        {
            itemsQuery = itemsQuery.Where(job => job.CompletedAt.HasValue && job.CompletedAt.Value <= query.CompletedTo.Value);
        }

        if (query.HasError.HasValue)
        {
            itemsQuery = query.HasError.Value
                ? itemsQuery.Where(job => job.ErrorMessage != null && job.ErrorMessage != string.Empty)
                : itemsQuery.Where(job => job.ErrorMessage == null || job.ErrorMessage == string.Empty);
        }

        itemsQuery = ApplyAdminSort(itemsQuery, query.SortBy, query.SortDescending);

        var totalCount = await itemsQuery.CountAsync(cancellationToken);
        var items = await itemsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<VideoProcessingJob>(items, totalCount, query.Page, query.PageSize);
    }

    private static IQueryable<VideoProcessingJob> ApplyAdminSort(
        IQueryable<VideoProcessingJob> query,
        string sortBy,
        bool sortDescending)
    {
        return (sortBy.Trim().ToLowerInvariant(), sortDescending) switch
        {
            ("createdat", true) => query.OrderByDescending(job => job.CreatedAt).ThenByDescending(job => job.Id),
            ("createdat", false) => query.OrderBy(job => job.CreatedAt).ThenBy(job => job.Id),
            ("startedat", true) => query.OrderByDescending(job => job.StartedAt).ThenByDescending(job => job.Id),
            ("startedat", false) => query.OrderBy(job => job.StartedAt).ThenBy(job => job.Id),
            ("completedat", true) => query.OrderByDescending(job => job.CompletedAt).ThenByDescending(job => job.Id),
            ("completedat", false) => query.OrderBy(job => job.CompletedAt).ThenBy(job => job.Id),
            ("progress", true) => query.OrderByDescending(job => job.Progress).ThenByDescending(job => job.Id),
            ("progress", false) => query.OrderBy(job => job.Progress).ThenBy(job => job.Id),
            ("videotitle", true) => query.OrderByDescending(job => job.Video.Title).ThenByDescending(job => job.Id),
            ("videotitle", false) => query.OrderBy(job => job.Video.Title).ThenBy(job => job.Id),
            _ => throw new ArgumentException("Unsupported video processing sortBy value.", nameof(sortBy))
        };
    }
}
