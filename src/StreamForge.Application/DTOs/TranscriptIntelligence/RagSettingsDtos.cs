namespace StreamForge.Application.DTOs.TranscriptIntelligence;

public sealed record SystemSecretStatusDto(
    bool IsConfigured,
    string? MaskedValue);

public sealed record QaProviderModelCatalogDto(
    string Provider,
    string DefaultModel,
    IReadOnlyCollection<string> Models);

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
