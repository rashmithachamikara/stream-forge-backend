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
}
