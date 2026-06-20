using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.Transcription;

public sealed class LocalFasterWhisperTranscriptionProvider : ITranscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly TranscriptionOptions _options;
    private readonly ILogger<LocalFasterWhisperTranscriptionProvider> _logger;

    public LocalFasterWhisperTranscriptionProvider(
        HttpClient httpClient,
        TranscriptionOptions options,
        ILogger<LocalFasterWhisperTranscriptionProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<TranscriptionSubmissionResult> SubmitAsync(
        TranscriptionProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new WorkerTranscriptionJobRequest(
            request.CorrelationId,
            request.VideoId.ToString(),
            new WorkerSourceReferenceDto(request.SourceReferenceType, request.SourceReferenceValue),
            request.Language,
            request.OutputFormats.ToArray(),
            new WorkerTranscriptionOptionsDto(
                request.Model,
                request.Device,
                request.ComputeType,
                request.BeamSize,
                request.EnableVad,
                request.EnableWordTimestamps),
            new WorkerCallbackOptionsDto(request.CallbackUrl, request.CallbackToken));

        using var response = await _httpClient.PostAsJsonAsync("/jobs/transcriptions", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var accepted = await response.Content.ReadFromJsonAsync<WorkerAcceptedResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Worker returned an empty acceptance response.");

        _logger.LogInformation(
            "Submitted transcription job for video {VideoId} to worker {WorkerBaseUrl}. Worker job ID: {WorkerJobId}",
            request.VideoId,
            _options.WorkerBaseUrl,
            accepted.JobId);

        return new TranscriptionSubmissionResult(accepted.JobId, accepted.Status);
    }

    public async Task<TranscriptionProviderJobStatus?> GetJobStatusAsync(
        string workerJobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workerJobId))
        {
            return null;
        }

        using var response = await _httpClient.GetAsync($"/jobs/transcriptions/{Uri.EscapeDataString(workerJobId)}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var status = await response.Content.ReadFromJsonAsync<WorkerJobStatusResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Worker returned an empty job status response.");

        return new TranscriptionProviderJobStatus(
            status.JobId,
            status.CorrelationId,
            status.Status,
            status.ProgressPercent,
            status.Stage,
            status.Message,
            status.Language,
            status.StartedAt,
            status.CompletedAt,
            status.MediaDurationSeconds,
            status.TranscribedUntilSeconds);
    }

    public async Task<TranscriptionProviderJobResult?> GetJobResultAsync(
        string workerJobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workerJobId))
        {
            return null;
        }

        using var response = await _httpClient.GetAsync($"/jobs/transcriptions/{Uri.EscapeDataString(workerJobId)}/result", cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WorkerJobResultResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Worker returned an empty job result response.");

        return new TranscriptionProviderJobResult(
            result.JobId,
            result.Artifacts.Select(artifact => new TranscriptionProviderArtifact(artifact.Kind, artifact.Path)).ToArray(),
            result.Language,
            result.SegmentsFilePath);
    }

    private sealed record WorkerTranscriptionJobRequest(
        string CorrelationId,
        string VideoId,
        WorkerSourceReferenceDto SourceReference,
        string? Language,
        string[] OutputFormats,
        WorkerTranscriptionOptionsDto Options,
        WorkerCallbackOptionsDto Callback);

    private sealed record WorkerSourceReferenceDto(
        string Type,
        string Value);

    private sealed record WorkerTranscriptionOptionsDto(
        string Model,
        string Device,
        string ComputeType,
        int BeamSize,
        bool EnableVad,
        bool EnableWordTimestamps);

    private sealed record WorkerCallbackOptionsDto(
        string Url,
        string? Token);

    private sealed record WorkerAcceptedResponse(
        string JobId,
        string Status)
    {
        [JsonPropertyName("jobId")]
        public string JobId { get; init; } = JobId;

        [JsonPropertyName("status")]
        public string Status { get; init; } = Status;
    }

    private sealed record WorkerJobStatusResponse(
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
        double? TranscribedUntilSeconds)
    {
        [JsonPropertyName("jobId")]
        public string JobId { get; init; } = JobId;

        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; init; } = CorrelationId;

        [JsonPropertyName("status")]
        public string Status { get; init; } = Status;

        [JsonPropertyName("progressPercent")]
        public int ProgressPercent { get; init; } = ProgressPercent;

        [JsonPropertyName("stage")]
        public string Stage { get; init; } = Stage;

        [JsonPropertyName("message")]
        public string? Message { get; init; } = Message;

        [JsonPropertyName("language")]
        public string? Language { get; init; } = Language;

        [JsonPropertyName("startedAt")]
        public DateTimeOffset? StartedAt { get; init; } = StartedAt;

        [JsonPropertyName("completedAt")]
        public DateTimeOffset? CompletedAt { get; init; } = CompletedAt;

        [JsonPropertyName("mediaDurationSeconds")]
        public double? MediaDurationSeconds { get; init; } = MediaDurationSeconds;

        [JsonPropertyName("transcribedUntilSeconds")]
        public double? TranscribedUntilSeconds { get; init; } = TranscribedUntilSeconds;
    }

    private sealed record WorkerJobResultResponse(
        string JobId,
        WorkerArtifactResponse[] Artifacts,
        string? SegmentsFilePath,
        string? Language)
    {
        [JsonPropertyName("jobId")]
        public string JobId { get; init; } = JobId;

        [JsonPropertyName("artifacts")]
        public WorkerArtifactResponse[] Artifacts { get; init; } = Artifacts;

        [JsonPropertyName("segmentsFilePath")]
        public string? SegmentsFilePath { get; init; } = SegmentsFilePath;

        [JsonPropertyName("language")]
        public string? Language { get; init; } = Language;
    }

    private sealed record WorkerArtifactResponse(
        string Kind,
        string Path)
    {
        [JsonPropertyName("kind")]
        public string Kind { get; init; } = Kind;

        [JsonPropertyName("path")]
        public string Path { get; init; } = Path;
    }
}
