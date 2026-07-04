using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.TranscriptIntelligence;

public sealed class AskVideoQuestionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ResolveRagSettingsService _resolveRagSettings;
    private readonly ITranscriptSearchProvider _transcriptSearchProvider;
    private readonly IVideoQuestionAnsweringProviderFactory _providerFactory;

    public AskVideoQuestionService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ResolveRagSettingsService resolveRagSettings,
        ITranscriptSearchProvider transcriptSearchProvider,
        IVideoQuestionAnsweringProviderFactory providerFactory)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _resolveRagSettings = resolveRagSettings;
        _transcriptSearchProvider = transcriptSearchProvider;
        _providerFactory = providerFactory;
    }

    public async Task<GroundedQuestionAnswerDto> Handle(
        Guid videoId,
        string question,
        string? language,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        QuestionAnsweringSupport.ValidateQuestion(question);

        var canView = await _authorizationService.CanViewVideoAsync(
            videoId,
            _currentUserService.UserId,
            _currentUserService.Role,
            shareToken,
            cancellationToken);

        if (!canView)
        {
            throw new System.UnauthorizedAccessException("You do not have access to this video.");
        }

        var settings = await _resolveRagSettings.Handle(cancellationToken);
        QuestionAnsweringSupport.EnsureVideoQuestionsEnabled(settings);

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);

        var evidence = await QuestionAnsweringSupport.RetrievePerVideoEvidenceAsync(
            _unitOfWork,
            _transcriptSearchProvider,
            settings,
            videoId,
            video.Title,
            question.Trim(),
            language,
            cancellationToken);

        if (evidence.Count == 0)
        {
            return QuestionAnsweringSupport.CreateNoAnswerResponse(question);
        }

        var provider = _providerFactory.Resolve(settings.QaProvider);
        var result = await provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                settings.QaProvider,
                QuestionAnsweringSupport.ResolveQaModel(settings),
                question.Trim(),
                evidence,
                settings.QaMaxCitations,
                settings.QaMaxOutputTokens,
                settings.QaTemperature),
            cancellationToken);

        return QuestionAnsweringSupport.MapResponse(question, evidence, result, settings.QaMaxCitations);
    }
}

public sealed class AskQuestionAcrossVideosService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ResolveRagSettingsService _resolveRagSettings;
    private readonly ITranscriptSearchProvider _transcriptSearchProvider;
    private readonly IVideoQuestionAnsweringProviderFactory _providerFactory;

    public AskQuestionAcrossVideosService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ResolveRagSettingsService resolveRagSettings,
        ITranscriptSearchProvider transcriptSearchProvider,
        IVideoQuestionAnsweringProviderFactory providerFactory)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _resolveRagSettings = resolveRagSettings;
        _transcriptSearchProvider = transcriptSearchProvider;
        _providerFactory = providerFactory;
    }

    public async Task<GroundedQuestionAnswerDto> Handle(
        string question,
        string? language,
        IReadOnlyCollection<Guid>? videoIds,
        CancellationToken cancellationToken)
    {
        QuestionAnsweringSupport.ValidateQuestion(question);

        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new System.UnauthorizedAccessException("You must be authenticated to ask questions across videos.");
        }

        var settings = await _resolveRagSettings.Handle(cancellationToken);
        QuestionAnsweringSupport.EnsureCrossVideoQuestionsEnabled(settings);

        var scopedVideoIds = videoIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var accessibleVideoIds = await _unitOfWork.Videos.GetAccessibleVideoIdsAsync(
            _currentUserService.UserId,
            _currentUserService.Role,
            scopedVideoIds,
            cancellationToken);

        if (accessibleVideoIds.Count == 0)
        {
            return QuestionAnsweringSupport.CreateNoAnswerResponse(question);
        }

        var evidence = await QuestionAnsweringSupport.RetrieveAcrossVideosEvidenceAsync(
            _unitOfWork,
            _transcriptSearchProvider,
            settings,
            accessibleVideoIds,
            question.Trim(),
            language,
            cancellationToken);

        if (evidence.Count == 0)
        {
            return QuestionAnsweringSupport.CreateNoAnswerResponse(question);
        }

        var provider = _providerFactory.Resolve(settings.QaProvider);
        var result = await provider.AnswerAsync(
            new GroundedQuestionAnsweringRequest(
                settings.QaProvider,
                QuestionAnsweringSupport.ResolveQaModel(settings),
                question.Trim(),
                evidence,
                settings.QaMaxCitations,
                settings.QaMaxOutputTokens,
                settings.QaTemperature),
            cancellationToken);

        return QuestionAnsweringSupport.MapResponse(question, evidence, result, settings.QaMaxCitations);
    }
}

internal static class QuestionAnsweringSupport
{
    private const string RetrievalMode = "hybrid";
    private const string NoAnswerMessage = "I couldn't find enough transcript evidence to answer that question.";

    public static void ValidateQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }
    }

    public static string ResolveQaModel(EffectiveRagSettings settings)
    {
        if (settings.QaProvider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
        {
            return settings.GeminiQaModel;
        }

        throw new ArgumentException($"Unsupported question answering provider '{settings.QaProvider}'.");
    }

    public static void EnsureVideoQuestionsEnabled(EffectiveRagSettings settings)
    {
        TranscriptHybridSearchComposer.EnsureHybridSearchEnabled(settings);

        if (!settings.VideoQuestionsEnabled)
        {
            throw new ArgumentException("Grounded video questions are disabled.");
        }
    }

    public static void EnsureCrossVideoQuestionsEnabled(EffectiveRagSettings settings)
    {
        TranscriptHybridSearchComposer.EnsureHybridSearchEnabled(settings);

        if (!settings.CrossVideoQuestionsEnabled)
        {
            throw new ArgumentException("Grounded cross-video questions are disabled.");
        }
    }

    public static async Task<IReadOnlyList<GroundedQuestionEvidenceChunk>> RetrievePerVideoEvidenceAsync(
        IUnitOfWork unitOfWork,
        ITranscriptSearchProvider transcriptSearchProvider,
        EffectiveRagSettings settings,
        Guid videoId,
        string videoTitle,
        string question,
        string? language,
        CancellationToken cancellationToken)
    {
        var normalizedLanguage = TranscriptHybridSearchComposer.NormalizeLanguage(language);
        var requestedWindow = Math.Max(settings.QaMaxContextChunks, 1);

        var lexicalCandidateCount = TranscriptHybridSearchComposer.CalculateLexicalCandidateCount(settings, requestedWindow, requestedWindow);
        var semanticCandidateCount = TranscriptHybridSearchComposer.CalculateSemanticCandidateCount(settings, requestedWindow, requestedWindow);
        var mergedCandidateCount = TranscriptHybridSearchComposer.CalculateMergedCandidateCount(settings, requestedWindow, requestedWindow);

        var lexicalResult = await unitOfWork.VideoTranscriptChunks.SearchLexicalByVideoAsync(
            videoId,
            question,
            normalizedLanguage,
            1,
            lexicalCandidateCount,
            lexicalCandidateCount,
            cancellationToken);

        var semanticResult = await transcriptSearchProvider.SearchSemanticAsync(
            new TranscriptSemanticSearchRequest(
                question,
                settings.EmbeddingProvider,
                settings.EmbeddingModel,
                normalizedLanguage,
                1,
                semanticCandidateCount,
                semanticCandidateCount,
                videoId,
                null),
            cancellationToken);

        var merged = TranscriptHybridSearchComposer.Merge(
            lexicalResult.Items,
            semanticResult.Items,
            settings.HybridLexicalWeight,
            settings.HybridSemanticWeight,
            mergedCandidateCount);

        return merged
            .Take(settings.QaMaxContextChunks)
            .Select(item => new GroundedQuestionEvidenceChunk(
                item.ChunkId,
                item.VideoId,
                string.IsNullOrWhiteSpace(item.VideoTitle) ? videoTitle : item.VideoTitle,
                item.TranscriptionId,
                item.Language,
                item.StartSeconds,
                item.EndSeconds,
                item.Content))
            .ToArray();
    }

    public static async Task<IReadOnlyList<GroundedQuestionEvidenceChunk>> RetrieveAcrossVideosEvidenceAsync(
        IUnitOfWork unitOfWork,
        ITranscriptSearchProvider transcriptSearchProvider,
        EffectiveRagSettings settings,
        IReadOnlyCollection<Guid> videoIds,
        string question,
        string? language,
        CancellationToken cancellationToken)
    {
        var normalizedLanguage = TranscriptHybridSearchComposer.NormalizeLanguage(language);
        var requestedWindow = Math.Max(settings.QaMaxContextChunks, 1);

        var lexicalCandidateCount = TranscriptHybridSearchComposer.CalculateLexicalCandidateCount(settings, requestedWindow, requestedWindow);
        var semanticCandidateCount = TranscriptHybridSearchComposer.CalculateSemanticCandidateCount(settings, requestedWindow, requestedWindow);
        var mergedCandidateCount = TranscriptHybridSearchComposer.CalculateMergedCandidateCount(settings, requestedWindow, requestedWindow);

        var lexicalResult = await unitOfWork.VideoTranscriptChunks.SearchLexicalAcrossVideosAsync(
            videoIds,
            question,
            normalizedLanguage,
            1,
            lexicalCandidateCount,
            lexicalCandidateCount,
            cancellationToken);

        var semanticResult = await transcriptSearchProvider.SearchSemanticAsync(
            new TranscriptSemanticSearchRequest(
                question,
                settings.EmbeddingProvider,
                settings.EmbeddingModel,
                normalizedLanguage,
                1,
                semanticCandidateCount,
                semanticCandidateCount,
                null,
                videoIds),
            cancellationToken);

        var merged = TranscriptHybridSearchComposer.Merge(
            lexicalResult.Items,
            semanticResult.Items,
            settings.HybridLexicalWeight,
            settings.HybridSemanticWeight,
            mergedCandidateCount);

        return merged
            .Take(settings.QaMaxContextChunks)
            .Select(item => new GroundedQuestionEvidenceChunk(
                item.ChunkId,
                item.VideoId,
                item.VideoTitle ?? string.Empty,
                item.TranscriptionId,
                item.Language,
                item.StartSeconds,
                item.EndSeconds,
                item.Content))
            .ToArray();
    }

    public static GroundedQuestionAnswerDto CreateNoAnswerResponse(string question, int usedChunkCount = 0) =>
        new(
            question.Trim(),
            RetrievalMode,
            NoAnswerMessage,
            usedChunkCount,
            []);

    public static GroundedQuestionAnswerDto MapResponse(
        string question,
        IReadOnlyCollection<GroundedQuestionEvidenceChunk> evidence,
        GroundedQuestionAnsweringResult result,
        int maxCitations)
    {
        if (!result.CanAnswer || string.IsNullOrWhiteSpace(result.Answer))
        {
            return CreateNoAnswerResponse(question, evidence.Count);
        }

        var evidenceByChunkId = evidence.ToDictionary(item => item.ChunkId);
        var normalizedCitationIds = result.CitedChunkIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedCitationIds.Length == 0)
        {
            throw new InvalidOperationException("Question answering provider returned an answer without citations.");
        }

        var missingCitationIds = normalizedCitationIds
            .Where(id => !evidenceByChunkId.ContainsKey(id))
            .ToArray();

        if (missingCitationIds.Length > 0)
        {
            throw new InvalidOperationException("Question answering provider returned citation IDs outside the grounded evidence set.");
        }

        var citations = normalizedCitationIds
            .Take(Math.Max(maxCitations, 1))
            .Select(id =>
            {
                var chunk = evidenceByChunkId[id];
                return new GroundedQuestionCitationDto(
                    chunk.VideoId,
                    chunk.VideoTitle,
                    chunk.TranscriptionId,
                    chunk.ChunkId,
                    chunk.StartSeconds,
                    chunk.EndSeconds,
                    chunk.Content);
            })
            .ToArray();

        return new GroundedQuestionAnswerDto(
            question.Trim(),
            RetrievalMode,
            result.Answer.Trim(),
            evidence.Count,
            citations);
    }
}
