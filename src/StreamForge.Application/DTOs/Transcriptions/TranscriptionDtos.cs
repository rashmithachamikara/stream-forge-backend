namespace StreamForge.Application.DTOs.Transcriptions;

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

public sealed record VideoTranscriptionArtifactDto(
    Guid Id,
    string Format,
    string Status,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

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

public sealed record RequestVideoTranscriptionRequestDto(
    string? Language,
    IReadOnlyCollection<string>? OutputFormats);

public sealed record TranscriptionCallbackArtifactDto(
    string Kind,
    string Path);

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
