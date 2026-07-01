using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public bool Enabled { get; set; }

    public bool SemanticSearchEnabled { get; set; }

    public bool VideoQuestionsEnabled { get; set; }

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

    [Required]
    public string QaProvider { get; set; } = "disabled";

    public string QaModel { get; set; } = string.Empty;

    [Range(1, 100)]
    public int QaMaxContextChunks { get; set; } = 8;

    [Range(1, 100)]
    public int QaMaxCitations { get; set; } = 5;
}
