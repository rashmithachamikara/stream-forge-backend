using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Exceptions;

namespace StreamForge.Infrastructure.TranscriptIntelligence;

public sealed class GeminiVideoQuestionAnsweringProvider : IVideoQuestionAnsweringProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly RagOptions _options;
    private readonly ILogger<GeminiVideoQuestionAnsweringProvider> _logger;

    public GeminiVideoQuestionAnsweringProvider(
        HttpClient httpClient,
        RagOptions options,
        ILogger<GeminiVideoQuestionAnsweringProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public string ProviderKey => "gemini";

    public async Task<GroundedQuestionAnsweringResult> AnswerAsync(
        GroundedQuestionAnsweringRequest request,
        CancellationToken cancellationToken = default)
    {
        var geminiOptions = _options.QaProviderConfigs.Gemini;
        if (string.IsNullOrWhiteSpace(geminiOptions.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ArgumentException("A Gemini model is required for question answering.", nameof(request));
        }

        var prompt = BuildPrompt(request);
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(request.Model)}:generateContent?key={Uri.EscapeDataString(geminiOptions.ApiKey)}";
        var payload = new GeminiGenerateContentRequest(
            [new GeminiContent("user", [new GeminiPart(prompt)])],
            new GeminiGenerationConfig(
                Math.Clamp(request.Temperature, 0d, 2d),
                Math.Max(request.MaxOutputTokens, 1),
                "application/json"));

        using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, _jsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleFailedResponseAsync(response, request.Model, cancellationToken);
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Gemini returned an empty response.");

        var rawText = ExtractText(apiResponse);
        var answerPayload = ParseStructuredResponse(rawText, request.Model, _logger);

        _logger.LogInformation(
            "Generated grounded answer through Gemini model {Model} with {EvidenceCount} evidence chunk(s).",
            request.Model,
            request.Evidence.Count);

        return new GroundedQuestionAnsweringResult(
            answerPayload.CanAnswer,
            answerPayload.Answer ?? string.Empty,
            (answerPayload.Citations ?? [])
                .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToArray(),
            ProviderKey,
            request.Model,
            rawText);
    }

    private static string BuildPrompt(GroundedQuestionAnsweringRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You answer questions using only the transcript evidence provided below.");
        builder.AppendLine("Do not use external knowledge. If the evidence is insufficient, say you cannot answer from the transcript evidence.");
        builder.AppendLine("Return JSON only with this shape:");
        builder.AppendLine("{\"canAnswer\": boolean, \"answer\": string, \"citations\": [\"chunk-guid\"]}");
        builder.AppendLine("Only cite chunk IDs from the evidence list. Do not invent timestamps, video IDs, titles, or excerpt text.");
        builder.AppendLine();
        builder.AppendLine("Question:");
        builder.AppendLine(request.Question.Trim());
        builder.AppendLine();
        builder.AppendLine("Evidence:");

        foreach (var chunk in request.Evidence)
        {
            builder.AppendLine(
                $"- chunkId: {chunk.ChunkId}; videoId: {chunk.VideoId}; videoTitle: {chunk.VideoTitle}; transcriptionId: {chunk.TranscriptionId}; language: {chunk.Language}; startSeconds: {chunk.StartSeconds}; endSeconds: {chunk.EndSeconds}; content: {chunk.Content}");
        }

        return builder.ToString();
    }

    private static string ExtractText(GeminiGenerateContentResponse response)
    {
        var text = response.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? [])
            .Select(part => part.Text)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini did not return structured text content.");
        }

        return text.Trim();
    }

    private static StructuredAnswerPayload ParseStructuredResponse(
        string rawText,
        string model,
        ILogger logger)
    {
        var normalized = StripCodeFence(rawText);

        logger.LogDebug(
            "Gemini structured response received for model {Model}. Raw preview: {RawPreview}. Normalized preview: {NormalizedPreview}.",
            model,
            CreatePreview(rawText),
            CreatePreview(normalized));

        try
        {
            var payload = JsonSerializer.Deserialize<StructuredAnswerPayload>(normalized, _jsonOptions)
                ?? throw new InvalidOperationException("Gemini returned an empty structured answer.");

            return payload;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Gemini returned malformed structured JSON for model {Model}. Raw preview: {RawPreview}. Normalized preview: {NormalizedPreview}.",
                model,
                CreatePreview(rawText),
                CreatePreview(normalized));

            throw new InvalidOperationException("Gemini returned invalid structured JSON.", exception);
        }
    }

    private static string StripCodeFence(string rawText)
    {
        var trimmed = rawText.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lines = trimmed.Split('\n');
        if (lines.Length < 3)
        {
            return trimmed;
        }

        return string.Join('\n', lines.Skip(1).Take(lines.Length - 2)).Trim();
    }

    private static string CreatePreview(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        const int maxLength = 1200;
        var normalizedWhitespace = value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

        return normalizedWhitespace.Length <= maxLength
            ? normalizedWhitespace
            : normalizedWhitespace[..maxLength] + "...<truncated>";
    }

    private async Task HandleFailedResponseAsync(
        HttpResponseMessage response,
        string model,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var preview = CreatePreview(body);

        switch ((int)response.StatusCode)
        {
            case 429:
            {
                var retryAfter = response.Headers.RetryAfter?.Delta;
                _logger.LogWarning(
                    "Gemini rate limited request for model {Model}. RetryAfter: {RetryAfter}. Body preview: {BodyPreview}.",
                    model,
                    retryAfter,
                    preview);

                throw new ExternalServiceThrottledException(
                    "Gemini",
                    "The question-answering provider is temporarily rate-limiting requests. Please try again shortly.",
                    retryAfter);
            }
            case 401:
            case 403:
                _logger.LogError(
                    "Gemini authentication/authorization failure for model {Model}. Status: {StatusCode}. Body preview: {BodyPreview}.",
                    model,
                    (int)response.StatusCode,
                    preview);
                throw new ExternalServiceException(
                    "Gemini",
                    "The question-answering provider rejected the request due to provider authentication or authorization settings.");
            default:
                if ((int)response.StatusCode >= 500)
                {
                    _logger.LogWarning(
                        "Gemini upstream failure for model {Model}. Status: {StatusCode}. Body preview: {BodyPreview}.",
                        model,
                        (int)response.StatusCode,
                        preview);
                    throw new ExternalServiceException(
                        "Gemini",
                        "The question-answering provider is temporarily unavailable. Please try again shortly.");
                }

                _logger.LogWarning(
                    "Gemini request failed for model {Model}. Status: {StatusCode}. Body preview: {BodyPreview}.",
                    model,
                    (int)response.StatusCode,
                    preview);
                throw new ExternalServiceException(
                    "Gemini",
                    $"The question-answering provider request failed with status {(int)response.StatusCode}.");
        }
    }

    private sealed record GeminiGenerateContentRequest(
        [property: JsonPropertyName("contents")] IReadOnlyCollection<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] IReadOnlyCollection<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens,
        [property: JsonPropertyName("responseMimeType")] string ResponseMimeType);

    private sealed record GeminiGenerateContentResponse(
        [property: JsonPropertyName("candidates")] GeminiCandidate[]? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiCandidateContent? Content);

    private sealed record GeminiCandidateContent(
        [property: JsonPropertyName("parts")] GeminiCandidatePart[]? Parts);

    private sealed record GeminiCandidatePart(
        [property: JsonPropertyName("text")] string? Text);

    private sealed record StructuredAnswerPayload(
        [property: JsonPropertyName("canAnswer")] bool CanAnswer,
        [property: JsonPropertyName("answer")] string? Answer,
        [property: JsonPropertyName("citations")] string[]? Citations);
}

public sealed class VideoQuestionAnsweringProviderFactory : IVideoQuestionAnsweringProviderFactory
{
    private readonly IReadOnlyDictionary<string, IVideoQuestionAnsweringProvider> _providers;

    public VideoQuestionAnsweringProviderFactory(IEnumerable<IVideoQuestionAnsweringProvider> providers)
    {
        _providers = providers.ToDictionary(
            provider => provider.ProviderKey,
            provider => provider,
            StringComparer.OrdinalIgnoreCase);
    }

    public IVideoQuestionAnsweringProvider Resolve(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider) || provider.Equals("disabled", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Question answering provider is disabled.");
        }

        if (_providers.TryGetValue(provider.Trim(), out var resolved))
        {
            return resolved;
        }

        throw new ArgumentException($"Unsupported question answering provider '{provider}'.");
    }
}
