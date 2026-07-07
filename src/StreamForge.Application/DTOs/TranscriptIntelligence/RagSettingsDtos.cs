namespace StreamForge.Application.DTOs.TranscriptIntelligence;

/// <summary>
/// Indicates whether a provider secret exists without exposing its plaintext value.
/// </summary>
public sealed record SystemSecretStatusDto(
    bool IsConfigured,
    string? MaskedValue);

/// <summary>
/// Lists the allowed question-answering models for a provider.
/// </summary>
public sealed record QaProviderModelCatalogDto(
    string Provider,
    string DefaultModel,
    IReadOnlyCollection<string> Models);

/// <summary>
/// Admin payload for effective runtime RAG settings and secret status.
/// </summary>
public sealed record AdminRagSettingsDto(
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
    int QaMaxContextChunks,
    int QaMaxCitations,
    double QaTemperature,
    int QaMaxOutputTokens,
    IReadOnlyCollection<QaProviderModelCatalogDto> QaModelCatalog,
    SystemSecretStatusDto GeminiApiKey,
    SystemSecretStatusDto GrokApiKey,
    SystemSecretStatusDto GroqApiKey);

/// <summary>
/// Request payload for updating admin-managed RAG settings and provider secrets.
/// </summary>
public sealed record UpdateAdminRagSettingsRequestDto(
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
    string? GeminiQaModel,
    string? GrokQaModel,
    string? GroqQaModel,
    int QaMaxContextChunks,
    int QaMaxCitations,
    double QaTemperature,
    int QaMaxOutputTokens,
    string? GeminiApiKey,
    bool ClearGeminiApiKey,
    string? GrokApiKey,
    bool ClearGrokApiKey,
    string? GroqApiKey,
    bool ClearGroqApiKey);
