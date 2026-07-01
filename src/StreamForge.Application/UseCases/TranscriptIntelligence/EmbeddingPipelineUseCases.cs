using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.TranscriptIntelligence;

internal static class RagSettingKeys
{
    public const string Enabled = "rag.enabled";
    public const string SemanticSearchEnabled = "rag.semanticSearch.enabled";
    public const string VideoQuestionsEnabled = "rag.videoQuestions.enabled";
    public const string CrossVideoQuestionsEnabled = "rag.crossVideoQuestions.enabled";
    public const string EmbeddingProvider = "rag.embedding.provider";
    public const string EmbeddingModel = "rag.embedding.model";
    public const string EmbeddingBatchSize = "rag.embedding.batchSize";
    public const string RetrievalDefaultMode = "rag.retrieval.defaultMode";
    public const string RetrievalSemanticTopK = "rag.retrieval.semanticTopK";
    public const string RetrievalFullTextTopK = "rag.retrieval.fullTextTopK";
    public const string QaProvider = "rag.qa.provider";
    public const string QaModel = "rag.qa.model";
    public const string QaMaxContextChunks = "rag.qa.maxContextChunks";
    public const string QaMaxCitations = "rag.qa.maxCitations";

    public static readonly string[] All =
    [
        Enabled,
        SemanticSearchEnabled,
        VideoQuestionsEnabled,
        CrossVideoQuestionsEnabled,
        EmbeddingProvider,
        EmbeddingModel,
        EmbeddingBatchSize,
        RetrievalDefaultMode,
        RetrievalSemanticTopK,
        RetrievalFullTextTopK,
        QaProvider,
        QaModel,
        QaMaxContextChunks,
        QaMaxCitations
    ];
}

public sealed record EffectiveRagSettings(
    bool Enabled,
    bool SemanticSearchEnabled,
    bool VideoQuestionsEnabled,
    bool CrossVideoQuestionsEnabled,
    string EmbeddingProvider,
    string EmbeddingModel,
    int EmbeddingBatchSize,
    string RetrievalDefaultMode,
    int SemanticTopK,
    int FullTextTopK,
    string QaProvider,
    string QaModel,
    int QaMaxContextChunks,
    int QaMaxCitations)
{
    public bool IsEmbeddingPipelineEnabled =>
        Enabled && (SemanticSearchEnabled || VideoQuestionsEnabled || CrossVideoQuestionsEnabled);
}

public sealed record TranscriptEmbeddingJobResult(
    bool Executed,
    string Reason,
    int ChunksProcessed,
    int BatchCount,
    int? VectorSize);

public sealed class ResolveRagSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RagOptions _defaults;

    public ResolveRagSettingsService(IUnitOfWork unitOfWork, RagOptions defaults)
    {
        _unitOfWork = unitOfWork;
        _defaults = defaults;
    }

    public async Task<EffectiveRagSettings> Handle(CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.SystemSettings.GetByKeysAsync(RagSettingKeys.All, cancellationToken);
        var map = settings.ToDictionary(setting => setting.Key, setting => setting.Value, StringComparer.Ordinal);

        return new EffectiveRagSettings(
            GetBool(map, RagSettingKeys.Enabled, _defaults.Enabled),
            GetBool(map, RagSettingKeys.SemanticSearchEnabled, _defaults.SemanticSearchEnabled),
            GetBool(map, RagSettingKeys.VideoQuestionsEnabled, _defaults.VideoQuestionsEnabled),
            GetBool(map, RagSettingKeys.CrossVideoQuestionsEnabled, _defaults.CrossVideoQuestionsEnabled),
            GetString(map, RagSettingKeys.EmbeddingProvider, _defaults.EmbeddingProvider),
            GetString(map, RagSettingKeys.EmbeddingModel, _defaults.EmbeddingModel),
            GetInt(map, RagSettingKeys.EmbeddingBatchSize, _defaults.EmbeddingBatchSize),
            GetString(map, RagSettingKeys.RetrievalDefaultMode, _defaults.RetrievalDefaultMode),
            GetInt(map, RagSettingKeys.RetrievalSemanticTopK, _defaults.SemanticTopK),
            GetInt(map, RagSettingKeys.RetrievalFullTextTopK, _defaults.FullTextTopK),
            GetString(map, RagSettingKeys.QaProvider, _defaults.QaProvider),
            GetString(map, RagSettingKeys.QaModel, _defaults.QaModel),
            GetInt(map, RagSettingKeys.QaMaxContextChunks, _defaults.QaMaxContextChunks),
            GetInt(map, RagSettingKeys.QaMaxCitations, _defaults.QaMaxCitations));
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> map, string key, bool fallback) =>
        map.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;

    private static int GetInt(IReadOnlyDictionary<string, string> map, string key, int fallback) =>
        map.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;

    private static string GetString(IReadOnlyDictionary<string, string> map, string key, string fallback) =>
        map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
}

public sealed class GenerateTranscriptEmbeddingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranscriptEmbeddingProvider _embeddingProvider;
    private readonly ResolveRagSettingsService _resolveRagSettings;
    private readonly ILogger<GenerateTranscriptEmbeddingsService> _logger;

    public GenerateTranscriptEmbeddingsService(
        IUnitOfWork unitOfWork,
        ITranscriptEmbeddingProvider embeddingProvider,
        ResolveRagSettingsService resolveRagSettings,
        ILogger<GenerateTranscriptEmbeddingsService> logger)
    {
        _unitOfWork = unitOfWork;
        _embeddingProvider = embeddingProvider;
        _resolveRagSettings = resolveRagSettings;
        _logger = logger;
    }

    public async Task<TranscriptEmbeddingJobResult> Handle(
        Guid videoId,
        string language,
        CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var settings = await _resolveRagSettings.Handle(cancellationToken);

        if (!settings.IsEmbeddingPipelineEnabled)
        {
            _logger.LogDebug(
                "Skipping transcript embedding generation for video {VideoId} language {Language} because the embedding pipeline is disabled.",
                videoId,
                normalizedLanguage);

            return new TranscriptEmbeddingJobResult(false, "Embedding pipeline is disabled.", 0, 0, null);
        }

        var chunks = await _unitOfWork.VideoTranscriptChunks.GetByVideoAndLanguageAsync(
            videoId,
            normalizedLanguage,
            cancellationToken);
        var orderedChunks = chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk.Content))
            .OrderBy(chunk => chunk.StartSeconds)
            .ThenBy(chunk => chunk.Id)
            .ToArray();

        if (orderedChunks.Length == 0)
        {
            _logger.LogInformation(
                "Skipping transcript embedding generation for video {VideoId} language {Language} because no transcript chunks were found.",
                videoId,
                normalizedLanguage);

            return new TranscriptEmbeddingJobResult(false, "No transcript chunks found.", 0, 0, null);
        }

        var batchSize = Math.Clamp(settings.EmbeddingBatchSize, 1, 200);
        var totalBatchCount = 0;
        int? vectorSize = null;

        foreach (var batch in Batch(orderedChunks, batchSize))
        {
            var request = new TranscriptEmbeddingRequest(
                settings.EmbeddingProvider,
                settings.EmbeddingModel,
                batch.Select(chunk => new TranscriptEmbeddingRequestItem(chunk.Id, chunk.Content)).ToArray());

            var result = await _embeddingProvider.GenerateEmbeddingsAsync(request, cancellationToken);
            ValidateResult(batch, result);

            if (vectorSize is null)
            {
                vectorSize = result.VectorSize;
            }
            else if (vectorSize.Value != result.VectorSize)
            {
                throw new InvalidOperationException("Embedding worker returned inconsistent vector sizes across batches.");
            }

            var itemsByChunkId = result.Items.ToDictionary(item => item.ChunkId, item => item, EqualityComparer<Guid>.Default);
            foreach (var chunk in batch)
            {
                var item = itemsByChunkId[chunk.Id];
                chunk.SetEmbedding(result.Provider, result.Model, item.Embedding);
                await _unitOfWork.VideoTranscriptChunks.UpdateAsync(chunk, cancellationToken);
            }

            totalBatchCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated embeddings for {ChunkCount} transcript chunk(s) across {BatchCount} batch(es) for video {VideoId} language {Language} using provider {Provider} model {Model}.",
            orderedChunks.Length,
            totalBatchCount,
            videoId,
            normalizedLanguage,
            settings.EmbeddingProvider,
            settings.EmbeddingModel);

        return new TranscriptEmbeddingJobResult(
            true,
            "Embeddings generated successfully.",
            orderedChunks.Length,
            totalBatchCount,
            vectorSize);
    }

    private static string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("Language is required.", nameof(language));
        }

        return language.Trim().ToLowerInvariant();
    }

    private static void ValidateResult(
        IReadOnlyCollection<VideoTranscriptChunk> batch,
        TranscriptEmbeddingBatchResult result)
    {
        if (result.VectorSize <= 0)
        {
            throw new InvalidOperationException("Embedding worker returned an invalid vector size.");
        }

        var expectedIds = batch.Select(chunk => chunk.Id).OrderBy(id => id).ToArray();
        var actualItems = result.Items.ToArray();
        var actualIds = actualItems.Select(item => item.ChunkId).OrderBy(id => id).ToArray();

        if (!expectedIds.SequenceEqual(actualIds))
        {
            throw new InvalidOperationException("Embedding worker response did not match the submitted chunk set.");
        }

        if (actualItems.Any(item => item.Embedding.Count != result.VectorSize))
        {
            throw new InvalidOperationException("Embedding worker returned inconsistent embedding dimensions.");
        }
    }

    private static IEnumerable<IReadOnlyList<VideoTranscriptChunk>> Batch(
        IReadOnlyList<VideoTranscriptChunk> chunks,
        int batchSize)
    {
        for (var index = 0; index < chunks.Count; index += batchSize)
        {
            var count = Math.Min(batchSize, chunks.Count - index);
            yield return chunks.Skip(index).Take(count).ToArray();
        }
    }
}
