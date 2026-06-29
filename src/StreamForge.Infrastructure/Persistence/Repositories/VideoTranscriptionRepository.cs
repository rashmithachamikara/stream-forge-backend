using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class VideoTranscriptionRepository : BaseRepository<VideoTranscription>, IVideoTranscriptionRepository
{
    public VideoTranscriptionRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<VideoTranscription>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        return await DbContext.VideoTranscriptions
            .Include(transcription => transcription.Video)
            .Where(transcription => transcription.VideoId == videoId)
            .OrderBy(transcription => transcription.Language)
            .ThenBy(transcription => transcription.Format)
            .ToListAsync(cancellationToken);
    }

    public Task<VideoTranscription?> GetWithVideoAsync(Guid transcriptionId, CancellationToken cancellationToken = default) =>
        DbContext.VideoTranscriptions
            .Include(transcription => transcription.Video)
            .FirstOrDefaultAsync(transcription => transcription.Id == transcriptionId, cancellationToken);

    public async Task<IReadOnlyList<VideoTranscription>> GetAllOrderedAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbContext.VideoTranscriptions
            .Include(transcription => transcription.Video)
            .OrderByDescending(transcription => transcription.CreatedAt)
            .ThenBy(transcription => transcription.Language)
            .ThenBy(transcription => transcription.Format)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoTranscription>> GetByVideoAndStatusAsync(
        Guid videoId,
        params TranscriptionStatus[] statuses)
    {
        return await DbContext.VideoTranscriptions
            .Where(transcription => transcription.VideoId == videoId && statuses.Contains(transcription.Status))
            .OrderBy(transcription => transcription.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<VideoTranscription>> GetByStatusesAsync(
        CancellationToken cancellationToken = default,
        params TranscriptionStatus[] statuses)
    {
        return await DbContext.VideoTranscriptions
            .Include(transcription => transcription.Video)
            .Where(transcription => statuses.Contains(transcription.Status))
            .OrderByDescending(transcription => transcription.CreatedAt)
            .ThenBy(transcription => transcription.Language)
            .ThenBy(transcription => transcription.Format)
            .ToListAsync(cancellationToken);
    }

    public async Task<VideoTranscription?> GetByVideoLanguageAndFormatAsync(
        Guid videoId,
        string language,
        string format,
        CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = language.Trim().ToLowerInvariant();
        var normalizedFormat = format.Trim().ToUpperInvariant();

        return await DbContext.VideoTranscriptions
            .Include(transcription => transcription.Video)
            .FirstOrDefaultAsync(
                transcription => transcription.VideoId == videoId &&
                                 transcription.Language == normalizedLanguage &&
                                 transcription.Format == normalizedFormat,
                cancellationToken);
    }

    public async Task<IReadOnlyList<VideoTranscription>> GetByWorkerJobIdAsync(
        string workerJobId,
        CancellationToken cancellationToken = default)
    {
        var normalizedWorkerJobId = workerJobId.Trim();

        return await DbContext.VideoTranscriptions
            .Include(transcription => transcription.Video)
            .Where(transcription => transcription.WorkerJobId == normalizedWorkerJobId)
            .OrderBy(transcription => transcription.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoTranscription>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedCorrelationId = correlationId.Trim();

        return await DbContext.VideoTranscriptions
            .Include(transcription => transcription.Video)
            .Where(transcription => transcription.CorrelationId == normalizedCorrelationId)
            .OrderBy(transcription => transcription.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoTranscription>> QueryAdminRowsAsync(
        AdminTranscriptionJobsQuery query,
        CancellationToken cancellationToken = default)
    {
        var itemsQuery = DbContext.VideoTranscriptions
            .AsNoTracking()
            .Include(transcription => transcription.Video)
            .AsQueryable();

        if (query.Status.HasValue)
        {
            itemsQuery = itemsQuery.Where(transcription => transcription.Status == query.Status.Value);
        }

        if (query.VideoId.HasValue)
        {
            itemsQuery = itemsQuery.Where(transcription => transcription.VideoId == query.VideoId.Value);
        }

        if (query.UploaderUserId.HasValue)
        {
            itemsQuery = itemsQuery.Where(transcription => transcription.Video.UploaderId == query.UploaderUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var normalizedSearch = query.Search.Trim();
            var pattern = $"%{normalizedSearch}%";
            var hasRowId = Guid.TryParse(normalizedSearch, out var parsedRowId);

            itemsQuery = itemsQuery.Where(transcription =>
                EF.Functions.ILike(transcription.Video.Title, pattern) ||
                transcription.WorkerJobId == normalizedSearch ||
                transcription.CorrelationId == normalizedSearch ||
                (hasRowId && transcription.Id == parsedRowId));
        }

        if (query.CreatedFrom.HasValue)
        {
            itemsQuery = itemsQuery.Where(transcription => transcription.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            itemsQuery = itemsQuery.Where(transcription => transcription.CreatedAt <= query.CreatedTo.Value);
        }

        if (query.HasError.HasValue)
        {
            itemsQuery = query.HasError.Value
                ? itemsQuery.Where(transcription => transcription.FailureReason != null && transcription.FailureReason != string.Empty)
                : itemsQuery.Where(transcription => transcription.FailureReason == null || transcription.FailureReason == string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(query.Provider))
        {
            var provider = query.Provider.Trim();
            itemsQuery = itemsQuery.Where(transcription => transcription.Source == provider);
        }

        if (!string.IsNullOrWhiteSpace(query.Language))
        {
            var language = query.Language.Trim().ToLowerInvariant();
            itemsQuery = itemsQuery.Where(transcription => transcription.Language == language);
        }

        if (!string.IsNullOrWhiteSpace(query.Format))
        {
            var format = query.Format.Trim().ToUpperInvariant();
            itemsQuery = itemsQuery.Where(transcription => transcription.Format == format);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var source = query.Source.Trim();
            itemsQuery = itemsQuery.Where(transcription => transcription.Source == source);
        }

        itemsQuery = ApplyAdminSort(itemsQuery, query.SortBy, query.SortDescending);

        return await itemsQuery.ToListAsync(cancellationToken);
    }

    private static IQueryable<VideoTranscription> ApplyAdminSort(
        IQueryable<VideoTranscription> query,
        string sortBy,
        bool sortDescending)
    {
        return (sortBy.Trim().ToLowerInvariant(), sortDescending) switch
        {
            ("createdat", true) => query.OrderByDescending(transcription => transcription.CreatedAt).ThenByDescending(transcription => transcription.Id),
            ("createdat", false) => query.OrderBy(transcription => transcription.CreatedAt).ThenBy(transcription => transcription.Id),
            ("updatedat", true) => query.OrderByDescending(transcription => transcription.UpdatedAt).ThenByDescending(transcription => transcription.Id),
            ("updatedat", false) => query.OrderBy(transcription => transcription.UpdatedAt).ThenBy(transcription => transcription.Id),
            ("language", true) => query.OrderByDescending(transcription => transcription.Language).ThenByDescending(transcription => transcription.Id),
            ("language", false) => query.OrderBy(transcription => transcription.Language).ThenBy(transcription => transcription.Id),
            ("status", true) => query.OrderByDescending(transcription => transcription.Status).ThenByDescending(transcription => transcription.Id),
            ("status", false) => query.OrderBy(transcription => transcription.Status).ThenBy(transcription => transcription.Id),
            ("videotitle", true) => query.OrderByDescending(transcription => transcription.Video.Title).ThenByDescending(transcription => transcription.Id),
            ("videotitle", false) => query.OrderBy(transcription => transcription.Video.Title).ThenBy(transcription => transcription.Id),
            _ => throw new ArgumentException("Unsupported transcription sortBy value.", nameof(sortBy))
        };
    }
}
