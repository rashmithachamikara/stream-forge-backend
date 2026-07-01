namespace StreamForge.Application.Interfaces;

public sealed record TranscriptEmbeddingRequestItem(
    Guid ChunkId,
    string Text);

public sealed record TranscriptEmbeddingRequest(
    string Provider,
    string Model,
    IReadOnlyCollection<TranscriptEmbeddingRequestItem> Items);

public sealed record TranscriptEmbeddingResultItem(
    Guid ChunkId,
    IReadOnlyList<float> Embedding);

public sealed record TranscriptEmbeddingBatchResult(
    string Provider,
    string Model,
    int VectorSize,
    IReadOnlyCollection<TranscriptEmbeddingResultItem> Items);

public interface ITranscriptEmbeddingProvider
{
    Task<TranscriptEmbeddingBatchResult> GenerateEmbeddingsAsync(
        TranscriptEmbeddingRequest request,
        CancellationToken cancellationToken = default);
}
