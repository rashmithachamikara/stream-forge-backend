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

public interface ITranscriptionProvider
{
    Task<TranscriptionSubmissionResult> SubmitAsync(
        TranscriptionProviderRequest request,
        CancellationToken cancellationToken = default);
}
