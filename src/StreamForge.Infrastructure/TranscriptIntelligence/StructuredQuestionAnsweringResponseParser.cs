using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace StreamForge.Infrastructure.TranscriptIntelligence;

internal static class StructuredQuestionAnsweringResponseParser
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static StructuredAnswerPayload Parse(
        string providerName,
        string rawText,
        string model,
        ILogger logger)
    {
        var normalized = StripCodeFence(rawText);

        logger.LogDebug(
            "{ProviderName} structured response received for model {Model}. Raw preview: {RawPreview}. Normalized preview: {NormalizedPreview}.",
            providerName,
            model,
            CreatePreview(rawText),
            CreatePreview(normalized));

        try
        {
            var payload = JsonSerializer.Deserialize<StructuredAnswerPayload>(normalized, _jsonOptions)
                ?? throw new InvalidOperationException($"{providerName} returned an empty structured answer.");

            return payload;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "{ProviderName} returned malformed structured JSON for model {Model}. Raw preview: {RawPreview}. Normalized preview: {NormalizedPreview}.",
                providerName,
                model,
                CreatePreview(rawText),
                CreatePreview(normalized));

            throw new InvalidOperationException($"{providerName} returned invalid structured JSON.", exception);
        }
    }

    public static string CreatePreview(string value)
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
}

internal sealed record StructuredAnswerPayload(
    bool CanAnswer,
    string? Answer,
    string[]? Citations);
