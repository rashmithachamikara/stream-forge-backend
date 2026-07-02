using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Interfaces;

public sealed record TranscriptSemanticSearchRequest(
    string Query,
    string EmbeddingProvider,
    string EmbeddingModel,
    string? Language,
    int Page,
    int PageSize,
    int CandidateCount,
    Guid? VideoId,
    IReadOnlyCollection<Guid>? VideoIds);

public interface ITranscriptSearchProvider
{
    Task<PagedQueryResult<TranscriptSemanticChunkMatch>> SearchSemanticAsync(
        TranscriptSemanticSearchRequest request,
        CancellationToken cancellationToken = default);
}
