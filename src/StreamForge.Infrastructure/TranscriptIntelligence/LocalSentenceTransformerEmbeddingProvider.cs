using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.TranscriptIntelligence;

public sealed class LocalSentenceTransformerEmbeddingProvider : ITranscriptEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly RagOptions _options;
    private readonly ILogger<LocalSentenceTransformerEmbeddingProvider> _logger;

    public LocalSentenceTransformerEmbeddingProvider(
        HttpClient httpClient,
        RagOptions options,
        ILogger<LocalSentenceTransformerEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<TranscriptEmbeddingBatchResult> GenerateEmbeddingsAsync(
        TranscriptEmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new WorkerEmbedRequest(
            request.Provider,
            request.Model,
            request.Items.Select(item => new WorkerEmbedItem(item.ChunkId, item.Text)).ToArray());

        using var response = await _httpClient.PostAsJsonAsync("/embed", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WorkerEmbedResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Embedding worker returned an empty response.");

        _logger.LogInformation(
            "Generated {ItemCount} embedding(s) through worker {WorkerBaseUrl} using provider {Provider} model {Model}.",
            result.Items.Length,
            _options.WorkerBaseUrl,
            result.Provider,
            result.Model);

        return new TranscriptEmbeddingBatchResult(
            result.Provider,
            result.Model,
            result.VectorSize,
            result.Items
                .Select(item => new TranscriptEmbeddingResultItem(item.ChunkId, item.Embedding))
                .ToArray());
    }

    private sealed record WorkerEmbedRequest(
        string Provider,
        string Model,
        WorkerEmbedItem[] Items);

    private sealed record WorkerEmbedItem(
        Guid ChunkId,
        string Text);

    private sealed record WorkerEmbedResponse(
        string Provider,
        string Model,
        int VectorSize,
        WorkerEmbedResultItem[] Items)
    {
        [JsonPropertyName("provider")]
        public string Provider { get; init; } = Provider;

        [JsonPropertyName("model")]
        public string Model { get; init; } = Model;

        [JsonPropertyName("vectorSize")]
        public int VectorSize { get; init; } = VectorSize;

        [JsonPropertyName("items")]
        public WorkerEmbedResultItem[] Items { get; init; } = Items;
    }

    private sealed record WorkerEmbedResultItem(
        Guid ChunkId,
        float[] Embedding)
    {
        [JsonPropertyName("chunkId")]
        public Guid ChunkId { get; init; } = ChunkId;

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; init; } = Embedding;
    }
}
