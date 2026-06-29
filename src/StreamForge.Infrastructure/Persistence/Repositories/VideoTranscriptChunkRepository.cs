using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class VideoTranscriptChunkRepository : BaseRepository<VideoTranscriptChunk>, IVideoTranscriptChunkRepository
{
    public VideoTranscriptChunkRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task DeleteByVideoAndLanguageAsync(Guid videoId, string language, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = language.Trim().ToLowerInvariant();
        var rows = await DbContext.VideoTranscriptChunks
            .Where(chunk => chunk.VideoId == videoId && chunk.Language == normalizedLanguage)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return;
        }

        DbContext.VideoTranscriptChunks.RemoveRange(rows);
    }

    public async Task<IReadOnlyList<VideoTranscriptChunk>> GetByTranscriptionIdAsync(
        Guid transcriptionId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.VideoTranscriptChunks
            .AsNoTracking()
            .Where(chunk => chunk.TranscriptionId == transcriptionId)
            .OrderBy(chunk => chunk.StartSeconds)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoTranscriptChunk>> GetByVideoAndLanguageAsync(
        Guid videoId,
        string language,
        CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = language.Trim().ToLowerInvariant();
        return await DbContext.VideoTranscriptChunks
            .AsNoTracking()
            .Where(chunk => chunk.VideoId == videoId && chunk.Language == normalizedLanguage)
            .OrderBy(chunk => chunk.StartSeconds)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedQueryResult<VideoTranscriptChunk>> SearchKeywordAsync(
        Guid videoId,
        string searchTerm,
        string? language,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm.Trim();
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();

        var query = DbContext.VideoTranscriptChunks
            .AsNoTracking()
            .Where(chunk => chunk.VideoId == videoId);

        if (!string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            query = query.Where(chunk => chunk.Language == normalizedLanguage);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var pattern = $"%{normalizedSearchTerm}%";
            query = query.Where(chunk => EF.Functions.ILike(chunk.Content, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(chunk => chunk.StartSeconds)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<VideoTranscriptChunk>(items, totalCount, page, pageSize);
    }
}
