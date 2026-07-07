namespace StreamForge.Application.Interfaces;

/// <summary>
/// Represents a transcript chunk supplied to grounded question-answering providers as evidence.
/// </summary>
public sealed record GroundedQuestionEvidenceChunk(
    Guid ChunkId,
    Guid VideoId,
    string VideoTitle,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content);

/// <summary>
/// Represents a grounded question-answering request built from retrieved transcript evidence.
/// </summary>
public sealed record GroundedQuestionAnsweringRequest(
    string Provider,
    string Model,
    string Question,
    IReadOnlyCollection<GroundedQuestionEvidenceChunk> Evidence,
    int MaxCitations,
    int MaxOutputTokens,
    double Temperature);

/// <summary>
/// Represents the normalized result returned by a grounded question-answering provider.
/// </summary>
public sealed record GroundedQuestionAnsweringResult(
    bool CanAnswer,
    string Answer,
    IReadOnlyCollection<Guid> CitedChunkIds,
    string Provider,
    string Model,
    string? RawResponse);

/// <summary>
/// Defines a provider-specific grounded question-answering implementation.
/// </summary>
public interface IVideoQuestionAnsweringProvider
{
    /// <summary>
    /// Gets the stable provider key used to resolve this implementation.
    /// </summary>
    string ProviderKey { get; }

    /// <summary>
    /// Produces a grounded answer using only the supplied transcript evidence.
    /// </summary>
    /// <param name="request">Grounded answering request with evidence and generation settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A normalized grounded answering result.</returns>
    Task<GroundedQuestionAnsweringResult> AnswerAsync(
        GroundedQuestionAnsweringRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves grounded question-answering providers by configured provider key.
/// </summary>
public interface IVideoQuestionAnsweringProviderFactory
{
    /// <summary>
    /// Resolves a grounded question-answering provider for the specified key.
    /// </summary>
    /// <param name="provider">Provider key to resolve.</param>
    /// <returns>The matching grounded question-answering provider.</returns>
    IVideoQuestionAnsweringProvider Resolve(string provider);
}
