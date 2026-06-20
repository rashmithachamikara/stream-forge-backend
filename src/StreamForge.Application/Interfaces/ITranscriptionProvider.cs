namespace StreamForge.Application.Interfaces;

public sealed record TranscriptionProviderRequest(
    Guid VideoId,
    string CorrelationId,
    string SourceReferenceType,
    string SourceReferenceValue,
    string? Language,
    IReadOnlyCollection<string> OutputFormats,
    string CallbackUrl,
    string? CallbackToken,
    string Model,
    string Device,
    string ComputeType,
    int BeamSize,
    bool EnableVad,
    bool EnableWordTimestamps);

public sealed record TranscriptionSubmissionResult(
    string JobId,
    string Status);

public sealed record TranscriptionProviderJobStatus(
    string JobId,
    string CorrelationId,
    string Status,
    int ProgressPercent,
    string Stage,
    string? Message,
    string? Language,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    double? MediaDurationSeconds,
    double? TranscribedUntilSeconds);

public sealed record TranscriptionProviderArtifact(
    string Kind,
    string Path);

public sealed record TranscriptionProviderJobResult(
    string JobId,
    IReadOnlyCollection<TranscriptionProviderArtifact> Artifacts,
    string? Language,
    string? SegmentsFilePath);

public interface ITranscriptionProvider
{
    Task<TranscriptionSubmissionResult> SubmitAsync(
        TranscriptionProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<TranscriptionProviderJobStatus?> GetJobStatusAsync(
        string workerJobId,
        CancellationToken cancellationToken = default);

    Task<TranscriptionProviderJobResult?> GetJobResultAsync(
        string workerJobId,
        CancellationToken cancellationToken = default);
}
