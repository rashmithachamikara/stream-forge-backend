using StreamForge.Application.Interfaces;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Infrastructure.TranscriptIntelligence;

public sealed class PostgresTranscriptSearchProvider : ITranscriptSearchProvider
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranscriptEmbeddingProvider _embeddingProvider;

    public PostgresTranscriptSearchProvider(
        IUnitOfWork unitOfWork,
        ITranscriptEmbeddingProvider embeddingProvider)
    {
        _unitOfWork = unitOfWork;
        _embeddingProvider = embeddingProvider;
    }

    public async Task<PagedQueryResult<TranscriptSemanticChunkMatch>> SearchSemanticAsync(
        TranscriptSemanticSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await GenerateQueryEmbeddingAsync(request, cancellationToken);

        if (request.VideoId.HasValue)
        {
            return await _unitOfWork.VideoTranscriptChunks.SearchSemanticByVideoAsync(
                request.VideoId.Value,
                queryEmbedding,
                request.EmbeddingProvider,
                request.EmbeddingModel,
                request.Language,
                request.Page,
                request.PageSize,
                request.CandidateCount,
                cancellationToken);
        }

        if (request.VideoIds is null || request.VideoIds.Count == 0)
        {
            return new PagedQueryResult<TranscriptSemanticChunkMatch>([], 0, request.Page, request.PageSize);
        }

        return await _unitOfWork.VideoTranscriptChunks.SearchSemanticAcrossVideosAsync(
            request.VideoIds,
            queryEmbedding,
            request.EmbeddingProvider,
            request.EmbeddingModel,
            request.Language,
            request.Page,
            request.PageSize,
            request.CandidateCount,
            cancellationToken);
    }

    private async Task<float[]> GenerateQueryEmbeddingAsync(
        TranscriptSemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        var chunkId = Guid.NewGuid();
        var result = await _embeddingProvider.GenerateEmbeddingsAsync(
            new TranscriptEmbeddingRequest(
                request.EmbeddingProvider,
                request.EmbeddingModel,
                [new TranscriptEmbeddingRequestItem(chunkId, request.Query)]),
            cancellationToken);

        var item = result.Items.SingleOrDefault();
        if (item is null || item.ChunkId != chunkId)
        {
            throw new InvalidOperationException("Embedding provider did not return a query embedding.");
        }

        if (item.Embedding.Count != result.VectorSize)
        {
            throw new InvalidOperationException("Embedding provider returned an invalid query embedding.");
        }

        return item.Embedding.ToArray();
    }
}
