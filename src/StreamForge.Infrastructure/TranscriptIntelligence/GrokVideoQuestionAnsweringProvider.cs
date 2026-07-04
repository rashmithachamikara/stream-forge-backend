using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Exceptions;

namespace StreamForge.Infrastructure.TranscriptIntelligence;

public sealed class GrokVideoQuestionAnsweringProvider : IVideoQuestionAnsweringProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly RagOptions _options;
    private readonly ILogger<GrokVideoQuestionAnsweringProvider> _logger;

    public GrokVideoQuestionAnsweringProvider(
        HttpClient httpClient,
        RagOptions options,
        ILogger<GrokVideoQuestionAnsweringProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public string ProviderKey => "grok";

    public async Task<GroundedQuestionAnsweringResult> AnswerAsync(
        GroundedQuestionAnsweringRequest request,
        CancellationToken cancellationToken = default)
    {
        var grokOptions = _options.QaProviderConfigs.Grok;
        if (string.IsNullOrWhiteSpace(grokOptions.ApiKey))
        {
            throw new InvalidOperationException("Grok API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ArgumentException("A Grok model is required for question answering.", nameof(request));
        }

        var prompt = BuildPrompt(request);
        var payload = new GrokChatCompletionsRequest(
            request.Model,
            [
                new GrokChatMessage("system", "You are a grounded transcript question answering assistant. Return JSON only."),
                new GrokChatMessage("user", prompt)
            ],
            Math.Clamp(request.Temperature, 0d, 2d),
            Math.Max(request.MaxOutputTokens, 1));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(payload, options: _jsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", grokOptions.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleFailedResponseAsync(response, request.Model, cancellationToken);
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<GrokChatCompletionsResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Grok returned an empty response.");

        var rawText = ExtractText(apiResponse);
        var answerPayload = StructuredQuestionAnsweringResponseParser.Parse("Grok", rawText, request.Model, _logger);

        _logger.LogInformation(
            "Generated grounded answer through Grok model {Model} with {EvidenceCount} evidence chunk(s).",
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
        var distinctVideoCount = request.Evidence
            .Select(chunk => chunk.VideoId)
            .Distinct()
            .Count();

        var sourceLabel = distinctVideoCount > 1 ? "the provided videos" : "the provided video";
        var builder = new StringBuilder();
        builder.AppendLine("You answer questions using only the transcript evidence provided below.");
        builder.AppendLine("Do not use external knowledge. If the evidence is insufficient, say you cannot answer from the transcript evidence.");
        builder.AppendLine($"When referring to the source material, prefer phrases like \"{sourceLabel}\" or the cited video title, and avoid calling it \"the provided transcript\".");
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

    private static string ExtractText(GrokChatCompletionsResponse response)
    {
        var text = response.Choices?
            .Select(choice => choice.Message?.Content)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Grok did not return structured text content.");
        }

        return text.Trim();
    }

    private async Task HandleFailedResponseAsync(
        HttpResponseMessage response,
        string model,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var preview = StructuredQuestionAnsweringResponseParser.CreatePreview(body);

        switch ((int)response.StatusCode)
        {
            case 429:
            {
                var retryAfter = response.Headers.RetryAfter?.Delta;
                _logger.LogWarning(
                    "Grok rate limited request for model {Model}. RetryAfter: {RetryAfter}. Body preview: {BodyPreview}.",
                    model,
                    retryAfter,
                    preview);

                throw new ExternalServiceThrottledException(
                    "Grok",
                    "The question-answering provider is temporarily rate-limiting requests. Please try again shortly.",
                    retryAfter);
            }
            case 401:
            case 403:
                _logger.LogError(
                    "Grok authentication/authorization failure for model {Model}. Status: {StatusCode}. Body preview: {BodyPreview}.",
                    model,
                    (int)response.StatusCode,
                    preview);
                throw new ExternalServiceException(
                    "Grok",
                    "The question-answering provider rejected the request due to provider authentication or authorization settings.");
            default:
                if ((int)response.StatusCode >= 500)
                {
                    _logger.LogWarning(
                        "Grok upstream failure for model {Model}. Status: {StatusCode}. Body preview: {BodyPreview}.",
                        model,
                        (int)response.StatusCode,
                        preview);
                    throw new ExternalServiceException(
                        "Grok",
                        "The question-answering provider is temporarily unavailable. Please try again shortly.");
                }

                _logger.LogWarning(
                    "Grok request failed for model {Model}. Status: {StatusCode}. Body preview: {BodyPreview}.",
                    model,
                    (int)response.StatusCode,
                    preview);
                throw new ExternalServiceException(
                    "Grok",
                    $"The question-answering provider request failed with status {(int)response.StatusCode}.");
        }
    }

    private sealed record GrokChatCompletionsRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyCollection<GrokChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record GrokChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record GrokChatCompletionsResponse(
        [property: JsonPropertyName("choices")] GrokChatChoice[]? Choices);

    private sealed record GrokChatChoice(
        [property: JsonPropertyName("message")] GrokChatChoiceMessage? Message);

    private sealed record GrokChatChoiceMessage(
        [property: JsonPropertyName("content")] string? Content);
}
