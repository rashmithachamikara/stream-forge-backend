namespace StreamForge.Application.DTOs.Transcriptions;

public sealed record VideoTranscriptionDto(
    Guid Id,
    Guid VideoId,
    string Language,
    string Format,
    string Status,
    string Source,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

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
