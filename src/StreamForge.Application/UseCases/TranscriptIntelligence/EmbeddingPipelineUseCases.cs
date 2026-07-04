using StreamForge.Application.Common;
using StreamForge.Application.DTOs.TranscriptIntelligence;
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
    public const string RetrievalHybridSemanticWeight = "rag.retrieval.hybridSemanticWeight";
    public const string RetrievalHybridLexicalWeight = "rag.retrieval.hybridLexicalWeight";
    public const string RetrievalHybridMaxCandidates = "rag.retrieval.hybridMaxCandidates";
    public const string QaProvider = "rag.qa.provider";
    public const string QaMaxContextChunks = "rag.qa.maxContextChunks";
    public const string QaMaxCitations = "rag.qa.maxCitations";
    public const string QaTemperature = "rag.qa.temperature";
    public const string QaMaxOutputTokens = "rag.qa.maxOutputTokens";

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
        RetrievalHybridSemanticWeight,
        RetrievalHybridLexicalWeight,
        RetrievalHybridMaxCandidates,
        QaProvider,
        QaMaxContextChunks,
        QaMaxCitations,
        QaTemperature,
        QaMaxOutputTokens
    ];
}

public static class RagSecretKeys
{
    public const string GeminiApiKey = "rag.qa.providers.gemini.apiKey";
    public const string GrokApiKey = "rag.qa.providers.grok.apiKey";
    public const string GroqApiKey = "rag.qa.providers.groq.apiKey";

    public static string GetProviderApiKey(string provider) =>
        provider.Trim().ToLowerInvariant() switch
        {
            "gemini" => GeminiApiKey,
            "grok" => GrokApiKey,
            "groq" => GroqApiKey,
            _ => throw new ArgumentException($"Unsupported question answering provider '{provider}'.", nameof(provider))
        };
}

internal static class RagEmbeddingModelDimensions
{
    private static readonly Dictionary<string, int> _knownDimensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sentence-transformers/all-MiniLM-L6-v2"] = 384
    };

    public static int? GetExpectedDimension(string model) =>
        _knownDimensions.TryGetValue(model.Trim(), out var dimension)
            ? dimension
            : null;
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
    double HybridSemanticWeight,
    double HybridLexicalWeight,
    int HybridMaxCandidates,
    string QaProvider,
    string GeminiQaModel,
    string GrokQaModel,
    string GroqQaModel,
    string? GeminiQaApiKey,
    string? GrokQaApiKey,
    string? GroqQaApiKey,
    int QaMaxContextChunks,
    int QaMaxCitations,
    double QaTemperature,
    int QaMaxOutputTokens)
{
    public bool IsEmbeddingPipelineEnabled =>
        Enabled && (SemanticSearchEnabled || VideoQuestionsEnabled || CrossVideoQuestionsEnabled);

    public string ResolveQaModel()
    {
        if (QaProvider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
        {
            return GeminiQaModel;
        }

        if (QaProvider.Equals("grok", StringComparison.OrdinalIgnoreCase))
        {
            return GrokQaModel;
        }

        if (QaProvider.Equals("groq", StringComparison.OrdinalIgnoreCase))
        {
            return GroqQaModel;
        }

        throw new ArgumentException($"Unsupported question answering provider '{QaProvider}'.");
    }

    public string? ResolveQaApiKey()
    {
        if (QaProvider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
        {
            return GeminiQaApiKey;
        }

        if (QaProvider.Equals("grok", StringComparison.OrdinalIgnoreCase))
        {
            return GrokQaApiKey;
        }

        if (QaProvider.Equals("groq", StringComparison.OrdinalIgnoreCase))
        {
            return GroqQaApiKey;
        }

        throw new ArgumentException($"Unsupported question answering provider '{QaProvider}'.");
    }
}

public sealed record TranscriptEmbeddingJobResult(
    bool Executed,
    string Reason,
    int ChunksProcessed,
    int BatchCount,
    int? VectorSize);

public sealed class GetAdminRagSettingsService
{
    private readonly ResolveRagSettingsService _resolveRagSettingsService;
    private readonly RagOptions _defaults;
    private readonly ISystemSecretStoreService _systemSecretStoreService;

    public GetAdminRagSettingsService(
        ResolveRagSettingsService resolveRagSettingsService,
        RagOptions defaults,
        ISystemSecretStoreService systemSecretStoreService)
    {
        _resolveRagSettingsService = resolveRagSettingsService;
        _defaults = defaults;
        _systemSecretStoreService = systemSecretStoreService;
    }

    public async Task<AdminRagSettingsDto> Handle(CancellationToken cancellationToken = default)
    {
        var settings = await _resolveRagSettingsService.Handle(cancellationToken);
        var geminiStatus = await _systemSecretStoreService.GetStatusAsync(
            RagSecretKeys.GeminiApiKey,
            _defaults.QaProviderConfigs.Gemini.ApiKey,
            cancellationToken);
        var grokStatus = await _systemSecretStoreService.GetStatusAsync(
            RagSecretKeys.GrokApiKey,
            _defaults.QaProviderConfigs.Grok.ApiKey,
            cancellationToken);
        var groqStatus = await _systemSecretStoreService.GetStatusAsync(
            RagSecretKeys.GroqApiKey,
            _defaults.QaProviderConfigs.Groq.ApiKey,
            cancellationToken);

        return new AdminRagSettingsDto(
            settings.Enabled,
            settings.SemanticSearchEnabled,
            settings.VideoQuestionsEnabled,
            settings.CrossVideoQuestionsEnabled,
            settings.EmbeddingProvider,
            settings.EmbeddingModel,
            settings.EmbeddingBatchSize,
            settings.RetrievalDefaultMode,
            settings.SemanticTopK,
            settings.FullTextTopK,
            settings.HybridSemanticWeight,
            settings.HybridLexicalWeight,
            settings.HybridMaxCandidates,
            settings.QaProvider,
            settings.QaMaxContextChunks,
            settings.QaMaxCitations,
            settings.QaTemperature,
            settings.QaMaxOutputTokens,
            new SystemSecretStatusDto(geminiStatus.IsConfigured, geminiStatus.MaskedValue),
            new SystemSecretStatusDto(grokStatus.IsConfigured, grokStatus.MaskedValue),
            new SystemSecretStatusDto(groqStatus.IsConfigured, groqStatus.MaskedValue));
    }
}

public sealed class UpdateAdminRagSettingsService
{
    private static readonly HashSet<string> _supportedQaProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "disabled",
        "gemini",
        "grok",
        "groq"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly GetAdminRagSettingsService _getAdminRagSettingsService;
    private readonly ISystemSecretStoreService _systemSecretStoreService;

    public UpdateAdminRagSettingsService(
        IUnitOfWork unitOfWork,
        GetAdminRagSettingsService getAdminRagSettingsService,
        ISystemSecretStoreService systemSecretStoreService)
    {
        _unitOfWork = unitOfWork;
        _getAdminRagSettingsService = getAdminRagSettingsService;
        _systemSecretStoreService = systemSecretStoreService;
    }

    public async Task<AdminRagSettingsDto> Handle(
        UpdateAdminRagSettingsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EmbeddingProvider))
        {
            throw new ArgumentException("Embedding provider is required.", nameof(request.EmbeddingProvider));
        }

        if (string.IsNullOrWhiteSpace(request.EmbeddingModel))
        {
            throw new ArgumentException("Embedding model is required.", nameof(request.EmbeddingModel));
        }

        if (string.IsNullOrWhiteSpace(request.RetrievalDefaultMode))
        {
            throw new ArgumentException("Retrieval default mode is required.", nameof(request.RetrievalDefaultMode));
        }

        if (string.IsNullOrWhiteSpace(request.QaProvider))
        {
            throw new ArgumentException("Question answering provider is required.", nameof(request.QaProvider));
        }

        var normalizedQaProvider = request.QaProvider.Trim().ToLowerInvariant();
        if (!_supportedQaProviders.Contains(normalizedQaProvider))
        {
            throw new ArgumentException($"Unsupported question answering provider '{request.QaProvider}'.", nameof(request.QaProvider));
        }

        await UpsertSettingAsync(RagSettingKeys.Enabled, request.Enabled.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.SemanticSearchEnabled, request.SemanticSearchEnabled.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.VideoQuestionsEnabled, request.VideoQuestionsEnabled.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.CrossVideoQuestionsEnabled, request.CrossVideoQuestionsEnabled.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.EmbeddingProvider, request.EmbeddingProvider.Trim(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.EmbeddingModel, request.EmbeddingModel.Trim(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.EmbeddingBatchSize, request.EmbeddingBatchSize.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.RetrievalDefaultMode, request.RetrievalDefaultMode.Trim().ToLowerInvariant(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.RetrievalSemanticTopK, request.SemanticTopK.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.RetrievalFullTextTopK, request.FullTextTopK.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.RetrievalHybridSemanticWeight, request.HybridSemanticWeight.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.RetrievalHybridLexicalWeight, request.HybridLexicalWeight.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.RetrievalHybridMaxCandidates, request.HybridMaxCandidates.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.QaProvider, normalizedQaProvider, cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.QaMaxContextChunks, request.QaMaxContextChunks.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.QaMaxCitations, request.QaMaxCitations.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.QaTemperature, request.QaTemperature.ToString(), cancellationToken);
        await UpsertSettingAsync(RagSettingKeys.QaMaxOutputTokens, request.QaMaxOutputTokens.ToString(), cancellationToken);

        await HandleSecretUpdateAsync(RagSecretKeys.GeminiApiKey, request.GeminiApiKey, request.ClearGeminiApiKey, cancellationToken);
        await HandleSecretUpdateAsync(RagSecretKeys.GrokApiKey, request.GrokApiKey, request.ClearGrokApiKey, cancellationToken);
        await HandleSecretUpdateAsync(RagSecretKeys.GroqApiKey, request.GroqApiKey, request.ClearGroqApiKey, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _getAdminRagSettingsService.Handle(cancellationToken);
    }

    private async Task UpsertSettingAsync(string key, string value, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.SystemSettings.GetByKeyAsync(key, cancellationToken);
        if (existing is null)
        {
            await _unitOfWork.SystemSettings.AddAsync(SystemSetting.Create(key, value), cancellationToken);
            return;
        }

        existing.SetValue(value);
        await _unitOfWork.SystemSettings.UpdateAsync(existing, cancellationToken);
    }

    private async Task HandleSecretUpdateAsync(
        string key,
        string? plaintextValue,
        bool clear,
        CancellationToken cancellationToken)
    {
        if (clear)
        {
            await _systemSecretStoreService.ClearAsync(key, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(plaintextValue))
        {
            await _systemSecretStoreService.SetAsync(key, plaintextValue, cancellationToken);
        }
    }
}

public sealed class ResolveRagSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RagOptions _defaults;
    private readonly ISystemSecretStoreService? _systemSecretStoreService;

    public ResolveRagSettingsService(
        IUnitOfWork unitOfWork,
        RagOptions defaults,
        ISystemSecretStoreService? systemSecretStoreService = null)
    {
        _unitOfWork = unitOfWork;
        _defaults = defaults;
        _systemSecretStoreService = systemSecretStoreService;
    }

    public async Task<EffectiveRagSettings> Handle(CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.SystemSettings.GetByKeysAsync(RagSettingKeys.All, cancellationToken);
        var map = settings.ToDictionary(setting => setting.Key, setting => setting.Value, StringComparer.Ordinal);

        var geminiApiKey = await ResolveSecretAsync(
            RagSecretKeys.GeminiApiKey,
            _defaults.QaProviderConfigs.Gemini.ApiKey,
            cancellationToken);
        var grokApiKey = await ResolveSecretAsync(
            RagSecretKeys.GrokApiKey,
            _defaults.QaProviderConfigs.Grok.ApiKey,
            cancellationToken);
        var groqApiKey = await ResolveSecretAsync(
            RagSecretKeys.GroqApiKey,
            _defaults.QaProviderConfigs.Groq.ApiKey,
            cancellationToken);

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
            GetDouble(map, RagSettingKeys.RetrievalHybridSemanticWeight, _defaults.HybridSemanticWeight),
            GetDouble(map, RagSettingKeys.RetrievalHybridLexicalWeight, _defaults.HybridLexicalWeight),
            GetInt(map, RagSettingKeys.RetrievalHybridMaxCandidates, _defaults.HybridMaxCandidates),
            GetString(map, RagSettingKeys.QaProvider, _defaults.QaProvider),
            _defaults.QaProviderConfigs.Gemini.Model,
            _defaults.QaProviderConfigs.Grok.Model,
            _defaults.QaProviderConfigs.Groq.Model,
            geminiApiKey,
            grokApiKey,
            groqApiKey,
            GetInt(map, RagSettingKeys.QaMaxContextChunks, _defaults.QaMaxContextChunks),
            GetInt(map, RagSettingKeys.QaMaxCitations, _defaults.QaMaxCitations),
            GetDouble(map, RagSettingKeys.QaTemperature, _defaults.QaTemperature),
            GetInt(map, RagSettingKeys.QaMaxOutputTokens, _defaults.QaMaxOutputTokens));
    }

    private Task<string?> ResolveSecretAsync(
        string key,
        string? fallbackValue,
        CancellationToken cancellationToken)
    {
        if (_systemSecretStoreService is null)
        {
            return Task.FromResult(string.IsNullOrWhiteSpace(fallbackValue) ? null : fallbackValue);
        }

        return _systemSecretStoreService.ResolveAsync(key, fallbackValue, cancellationToken);
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> map, string key, bool fallback) =>
        map.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;

    private static int GetInt(IReadOnlyDictionary<string, string> map, string key, int fallback) =>
        map.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;

    private static double GetDouble(IReadOnlyDictionary<string, string> map, string key, double fallback) =>
        map.TryGetValue(key, out var value) && double.TryParse(value, out var parsed)
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

            var expectedDimension = RagEmbeddingModelDimensions.GetExpectedDimension(settings.EmbeddingModel);
            if (expectedDimension.HasValue && expectedDimension.Value != result.VectorSize)
            {
                throw new InvalidOperationException(
                    $"Embedding worker returned vector size {result.VectorSize} for model '{settings.EmbeddingModel}', but the application expects {expectedDimension.Value}.");
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
