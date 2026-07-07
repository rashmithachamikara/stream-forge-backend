namespace StreamForge.Application.DTOs.Transcriptions;

/// <summary>
/// Query parameters for the admin transcription-jobs endpoint.
/// </summary>
public sealed class AdminTranscriptionJobsQueryDto
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string? Status { get; init; }
    public Guid? VideoId { get; init; }
    public Guid? UploaderUserId { get; init; }
    public string? Search { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public bool? HasError { get; init; }
    public string? Provider { get; init; }
    public string? Language { get; init; }
    public string? Format { get; init; }
    public string? Source { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}

/// <summary>
/// User-facing transcription artifact metadata.
/// </summary>
public sealed record VideoTranscriptionDto(
    Guid Id,
    Guid VideoId,
    string Language,
    string Format,
    string Status,
    string Source,
    string? CorrelationId,
    string? WorkerJobId,
    string? Model,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    TranscriptionLiveStatusDto? LiveStatus);

/// <summary>
/// Provider-reported live transcription job status.
/// </summary>
public sealed record TranscriptionLiveStatusDto(
    string Status,
    int ProgressPercent,
    string Stage,
    string? Message,
    string? Language,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    double? MediaDurationSeconds,
    double? TranscribedUntilSeconds);

/// <summary>
/// Metadata for a single transcription artifact in a grouped job response.
/// </summary>
public sealed record VideoTranscriptionArtifactDto(
    Guid Id,
    string Format,
    string Status,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Video-scoped grouped transcription job payload.
/// </summary>
public sealed record VideoTranscriptionJobDto(
    string JobKey,
    Guid VideoId,
    string Language,
    string Status,
    string Source,
    string? CorrelationId,
    string? WorkerJobId,
    string? Model,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    TranscriptionLiveStatusDto? LiveStatus,
    IReadOnlyList<VideoTranscriptionArtifactDto> Artifacts);

/// <summary>
/// Admin grouped transcription job payload with video title context.
/// </summary>
public sealed record AdminTranscriptionJobDto(
    string JobKey,
    Guid VideoId,
    string VideoTitle,
    string Language,
    string Status,
    string Source,
    string? CorrelationId,
    string? WorkerJobId,
    string? Model,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    TranscriptionLiveStatusDto? LiveStatus,
    IReadOnlyList<VideoTranscriptionArtifactDto> Artifacts);

/// <summary>
/// Request payload for starting transcription for a video.
/// </summary>
public sealed record RequestVideoTranscriptionRequestDto(
    string? Language,
    IReadOnlyCollection<string>? OutputFormats);

/// <summary>
/// Keyword transcript search result for a single chunk.
/// </summary>
public sealed record TranscriptSearchResultDto(
    Guid ChunkId,
    Guid VideoId,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content);

/// <summary>
/// Semantic transcript search result for a single chunk.
/// </summary>
public sealed record TranscriptSemanticSearchResultDto(
    Guid ChunkId,
    Guid VideoId,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content,
    double Score);

/// <summary>
/// Cross-video semantic transcript search result.
/// </summary>
public sealed record CrossVideoTranscriptSemanticSearchResultDto(
    Guid ChunkId,
    Guid VideoId,
    string VideoTitle,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content,
    double Score);

/// <summary>
/// Hybrid lexical-plus-semantic transcript search result.
/// </summary>
public sealed record TranscriptHybridSearchResultDto(
    Guid ChunkId,
    Guid VideoId,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content,
    double Score,
    double? LexicalScore,
    double? SemanticScore);

/// <summary>
/// Cross-video hybrid transcript search result.
/// </summary>
public sealed record CrossVideoTranscriptHybridSearchResultDto(
    Guid ChunkId,
    Guid VideoId,
    string VideoTitle,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content,
    double Score,
    double? LexicalScore,
    double? SemanticScore);

/// <summary>
/// Request payload for asking a grounded question about a single video.
/// </summary>
public sealed record AskVideoQuestionRequestDto(
    string Question,
    string? Language);

/// <summary>
/// Request payload for asking a grounded question across multiple videos.
/// </summary>
public sealed record AskQuestionAcrossVideosRequestDto(
    string Question,
    string? Language,
    IReadOnlyCollection<Guid>? VideoIds);

/// <summary>
/// Citation payload for grounded question-answering responses.
/// </summary>
public sealed record GroundedQuestionCitationDto(
    Guid VideoId,
    string VideoTitle,
    Guid TranscriptionId,
    Guid ChunkId,
    double StartSeconds,
    double EndSeconds,
    string Content);

/// <summary>
/// Grounded question-answering response with cited transcript evidence.
/// </summary>
public sealed record GroundedQuestionAnswerDto(
    string Question,
    string RetrievalMode,
    string Answer,
    int UsedChunkCount,
    IReadOnlyList<GroundedQuestionCitationDto> Citations);

/// <summary>
/// Structured transcript chunk payload for transcript reading UIs.
/// </summary>
public sealed record TranscriptChunkDto(
    Guid ChunkId,
    Guid VideoId,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content);

/// <summary>
/// Admin transcription settings payload.
/// </summary>
public sealed record AdminTranscriptionSettingsDto(
    bool Enabled,
    bool AutoTranscribeOnReady,
    string Provider,
    string? DefaultLanguage,
    IReadOnlyList<string> OutputFormats,
    string Model,
    string Device,
    string ComputeType,
    int BeamSize,
    bool EnableVad,
    bool EnableWordTimestamps);

/// <summary>
/// Request payload for updating admin transcription settings.
/// </summary>
public sealed record UpdateAdminTranscriptionSettingsRequestDto(
    bool Enabled,
    bool AutoTranscribeOnReady,
    string Provider,
    string? DefaultLanguage,
    IReadOnlyCollection<string> OutputFormats,
    string Model,
    string Device,
    string ComputeType,
    int BeamSize,
    bool EnableVad,
    bool EnableWordTimestamps);

/// <summary>
/// Worker callback artifact descriptor.
/// </summary>
public sealed record TranscriptionCallbackArtifactDto(
    string Kind,
    string Path);

/// <summary>
/// Request payload posted by the transcription worker callback.
/// </summary>
public sealed record TranscriptionCallbackRequestDto(
    string CorrelationId,
    Guid VideoId,
    string WorkerJobId,
    string Status,
    string? Language,
    IReadOnlyCollection<TranscriptionCallbackArtifactDto> Artifacts,
    string? FailureReason,
    string? Provider,
    string? Model);
