using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

public interface IVideoTranscriptChunkRepository : IRepository<VideoTranscriptChunk>
{
    Task<PagedQueryResult<TranscriptLexicalChunkMatch>> SearchLexicalByVideoAsync(
        Guid videoId,
        string searchTerm,
        string? language,
        int page,
        int pageSize,
        int candidateCount,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<TranscriptLexicalChunkMatch>> SearchLexicalAcrossVideosAsync(
        IReadOnlyCollection<Guid> videoIds,
        string searchTerm,
        string? language,
        int page,
        int pageSize,
        int candidateCount,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<TranscriptSemanticChunkMatch>> SearchSemanticByVideoAsync(
        Guid videoId,
        float[] queryEmbedding,
        string embeddingProvider,
        string embeddingModel,
        string? language,
        int page,
        int pageSize,
        int candidateCount,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<TranscriptSemanticChunkMatch>> SearchSemanticAcrossVideosAsync(
        IReadOnlyCollection<Guid> videoIds,
        float[] queryEmbedding,
        string embeddingProvider,
        string embeddingModel,
        string? language,
        int page,
        int pageSize,
        int candidateCount,
        CancellationToken cancellationToken = default);

    Task DeleteByVideoAndLanguageAsync(Guid videoId, string language, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscriptChunk>> GetByTranscriptionIdAsync(
        Guid transcriptionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscriptChunk>> GetByVideoAndLanguageAsync(
        Guid videoId,
        string language,
        CancellationToken cancellationToken = default);

    Task<PagedQueryResult<VideoTranscriptChunk>> SearchFullTextAsync(
        Guid videoId,
        string searchTerm,
        string? language,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed record TranscriptLexicalChunkMatch(
    Guid ChunkId,
    Guid VideoId,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content,
    double Score,
    string? VideoTitle);

public sealed record TranscriptSemanticChunkMatch(
    Guid ChunkId,
    Guid VideoId,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content,
    double Score,
    string? VideoTitle);
