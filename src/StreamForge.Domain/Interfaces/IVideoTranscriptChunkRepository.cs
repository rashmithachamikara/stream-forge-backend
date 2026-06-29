using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

public interface IVideoTranscriptChunkRepository : IRepository<VideoTranscriptChunk>
{
    Task DeleteByVideoAndLanguageAsync(Guid videoId, string language, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscriptChunk>> GetByTranscriptionIdAsync(
        Guid transcriptionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscriptChunk>> GetByVideoAndLanguageAsync(
        Guid videoId,
        string language,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<VideoTranscriptChunk>> SearchKeywordAsync(
        Guid videoId,
        string searchTerm,
        string? language,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
