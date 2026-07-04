namespace StreamForge.Application.Interfaces;

public sealed record GroundedQuestionEvidenceChunk(
    Guid ChunkId,
    Guid VideoId,
    string VideoTitle,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content);

public sealed record GroundedQuestionAnsweringRequest(
    string Provider,
    string Model,
    string Question,
    IReadOnlyCollection<GroundedQuestionEvidenceChunk> Evidence,
    int MaxCitations,
    int MaxOutputTokens,
    double Temperature);

public sealed record GroundedQuestionAnsweringResult(
    bool CanAnswer,
    string Answer,
    IReadOnlyCollection<Guid> CitedChunkIds,
    string Provider,
    string Model,
    string? RawResponse);

public interface IVideoQuestionAnsweringProvider
{
    string ProviderKey { get; }

    Task<GroundedQuestionAnsweringResult> AnswerAsync(
        GroundedQuestionAnsweringRequest request,
        CancellationToken cancellationToken = default);
}

public interface IVideoQuestionAnsweringProviderFactory
{
    IVideoQuestionAnsweringProvider Resolve(string provider);
}
