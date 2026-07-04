namespace StreamForge.Application.DTOs.TranscriptIntelligence;

public sealed record SystemSecretStatusDto(
    bool IsConfigured,
    string? MaskedValue);

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
    int QaMaxContextChunks,
    int QaMaxCitations,
    double QaTemperature,
    int QaMaxOutputTokens,
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
