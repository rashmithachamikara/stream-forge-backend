namespace StreamForge.Application.Interfaces;

/// <summary>
/// Represents a single transcript chunk to embed.
/// </summary>
public sealed record TranscriptEmbeddingRequestItem(
    Guid ChunkId,
    string Text);

/// <summary>
/// Represents a batch embedding generation request.
/// </summary>
public sealed record TranscriptEmbeddingRequest(
    string Provider,
    string Model,
    IReadOnlyCollection<TranscriptEmbeddingRequestItem> Items);

/// <summary>
/// Represents an embedding result for a single transcript chunk.
/// </summary>
public sealed record TranscriptEmbeddingResultItem(
    Guid ChunkId,
    IReadOnlyList<float> Embedding);

/// <summary>
/// Represents a batch embedding response from an embedding provider.
/// </summary>
public sealed record TranscriptEmbeddingBatchResult(
    string Provider,
    string Model,
    int VectorSize,
    IReadOnlyCollection<TranscriptEmbeddingResultItem> Items);

/// <summary>
/// Generates embeddings for transcript content.
/// </summary>
public interface ITranscriptEmbeddingProvider
{
    /// <summary>
    /// Generates embeddings for the supplied transcript items.
    /// </summary>
    /// <param name="request">Embedding batch request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding batch response.</returns>
    Task<TranscriptEmbeddingBatchResult> GenerateEmbeddingsAsync(
        TranscriptEmbeddingRequest request,
        CancellationToken cancellationToken = default);
}
