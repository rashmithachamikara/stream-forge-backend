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
            .Where(transcription => transcription.VideoId == videoId)
            .OrderBy(transcription => transcription.Language)
            .ThenBy(transcription => transcription.Format)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoTranscription>> GetAllOrderedAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbContext.VideoTranscriptions
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
            .Where(transcription => transcription.CorrelationId == normalizedCorrelationId)
            .OrderBy(transcription => transcription.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
