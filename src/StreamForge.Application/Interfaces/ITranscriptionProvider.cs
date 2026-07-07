namespace StreamForge.Application.Interfaces;

/// <summary>
/// Represents a transcription submission request sent to an external worker or provider.
/// </summary>
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

/// <summary>
/// Represents the immediate result of submitting a transcription job.
/// </summary>
public sealed record TranscriptionSubmissionResult(
    string JobId,
    string Status);

/// <summary>
/// Represents a provider-reported live status update for a transcription job.
/// </summary>
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

/// <summary>
/// Represents a generated artifact reported by the transcription provider.
/// </summary>
public sealed record TranscriptionProviderArtifact(
    string Kind,
    string Path);

/// <summary>
/// Represents the final result payload for a completed transcription job.
/// </summary>
public sealed record TranscriptionProviderJobResult(
    string JobId,
    IReadOnlyCollection<TranscriptionProviderArtifact> Artifacts,
    string? Language,
    string? SegmentsFilePath);

/// <summary>
/// Submits, monitors, and reads results from a transcription provider.
/// </summary>
public interface ITranscriptionProvider
{
    /// <summary>
    /// Submits a new transcription job.
    /// </summary>
    /// <param name="request">Provider submission request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider submission result.</returns>
    Task<TranscriptionSubmissionResult> SubmitAsync(
        TranscriptionProviderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets live status for a previously submitted transcription job.
    /// </summary>
    /// <param name="workerJobId">Provider-specific job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current provider job status, or <see langword="null"/> when unavailable.</returns>
    Task<TranscriptionProviderJobStatus?> GetJobStatusAsync(
        string workerJobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the completed result payload for a transcription job when available.
    /// </summary>
    /// <param name="workerJobId">Provider-specific job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completed job result, or <see langword="null"/> when not yet available.</returns>
    Task<TranscriptionProviderJobResult?> GetJobResultAsync(
        string workerJobId,
        CancellationToken cancellationToken = default);
}
