using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Interfaces;

/// <summary>
/// Describes a semantic transcript search request over one or more videos.
/// </summary>
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

/// <summary>
/// Executes transcript retrieval against the underlying search store.
/// </summary>
public interface ITranscriptSearchProvider
{
    /// <summary>
    /// Searches transcript chunks using semantic retrieval.
    /// </summary>
    /// <param name="request">Semantic search request and scope information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged set of ranked transcript chunk matches.</returns>
    Task<PagedQueryResult<TranscriptSemanticChunkMatch>> SearchSemanticAsync(
        TranscriptSemanticSearchRequest request,
        CancellationToken cancellationToken = default);
}
