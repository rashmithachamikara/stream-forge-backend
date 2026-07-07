using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

/// <summary>
/// Configures transcript intelligence, retrieval, and grounded question-answering behavior.
/// </summary>
public sealed class RagOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Rag";

    /// <summary>
    /// Gets or sets whether transcript intelligence features are enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether semantic transcript search is enabled.
    /// </summary>
    public bool SemanticSearchEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether per-video grounded questions are enabled.
    /// </summary>
    public bool VideoQuestionsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether cross-video grounded questions are enabled.
    /// </summary>
    public bool CrossVideoQuestionsEnabled { get; set; }

    [Required]
    public string WorkerBaseUrl { get; set; } = "http://127.0.0.1:8091";

    [Range(1, 240)]
    public int JobTimeoutMinutes { get; set; } = 30;

    [Required]
    public string EmbeddingProvider { get; set; } = "local-sentence-transformer";

    [Required]
    public string EmbeddingModel { get; set; } = "sentence-transformers/all-MiniLM-L6-v2";

    [Range(1, 500)]
    public int EmbeddingBatchSize { get; set; } = 100;

    [Required]
    public string RetrievalDefaultMode { get; set; } = "hybrid";

    [Range(1, 1000)]
    public int SemanticTopK { get; set; } = 8;

    [Range(1, 1000)]
    public int FullTextTopK { get; set; } = 8;

    [Range(0, 1)]
    public double HybridSemanticWeight { get; set; } = 0.6d;

    [Range(0, 1)]
    public double HybridLexicalWeight { get; set; } = 0.4d;

    [Range(1, 1000)]
    public int HybridMaxCandidates { get; set; } = 12;

    [Required]
    public string QaProvider { get; set; } = "disabled";

    [Range(1, 100)]
    public int QaMaxContextChunks { get; set; } = 8;

    [Range(1, 100)]
    public int QaMaxCitations { get; set; } = 5;

    [Range(0, 2)]
    public double QaTemperature { get; set; } = 0d;

    [Range(1, 8192)]
    public int QaMaxOutputTokens { get; set; } = 512;

    public RagQaProviderConfigs QaProviderConfigs { get; set; } = new();
}

/// <summary>
/// Groups provider-specific question-answering configuration.
/// </summary>
public sealed class RagQaProviderConfigs
{
    public RagGeminiQaOptions Gemini { get; set; } = new();
    public RagGrokQaOptions Grok { get; set; } = new();
    public RagGroqQaOptions Groq { get; set; } = new();
}

/// <summary>
/// Configures Gemini-specific grounded question-answering connectivity.
/// </summary>
public sealed class RagGeminiQaOptions
{
    [Required]
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    [Required]
    public string Model { get; set; } = "gemini-2.5-flash";

    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Configures Grok-specific grounded question-answering connectivity.
/// </summary>
public sealed class RagGrokQaOptions
{
    [Required]
    public string BaseUrl { get; set; } = "https://api.x.ai";

    [Required]
    public string Model { get; set; } = "grok-3-mini";

    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Configures Groq-specific grounded question-answering connectivity.
/// </summary>
public sealed class RagGroqQaOptions
{
    [Required]
    public string BaseUrl { get; set; } = "https://api.groq.com";

    [Required]
    public string Model { get; set; } = "llama-3.1-8b-instant";

    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 60;
}
