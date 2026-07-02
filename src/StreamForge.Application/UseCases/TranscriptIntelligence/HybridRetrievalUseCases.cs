using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.TranscriptIntelligence;

public sealed class SearchVideoTranscriptHybridService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ResolveRagSettingsService _resolveRagSettings;
    private readonly ITranscriptSearchProvider _transcriptSearchProvider;

    public SearchVideoTranscriptHybridService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ResolveRagSettingsService resolveRagSettings,
        ITranscriptSearchProvider transcriptSearchProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _resolveRagSettings = resolveRagSettings;
        _transcriptSearchProvider = transcriptSearchProvider;
    }

    public async Task<PagedResponseDto<TranscriptHybridSearchResultDto>> Handle(
        Guid videoId,
        string query,
        string? language,
        int page,
        int pageSize,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        var canView = await _authorizationService.CanViewVideoAsync(
            videoId,
            _currentUserService.UserId,
            _currentUserService.Role,
            shareToken,
            cancellationToken);

        if (!canView)
        {
            throw new UnauthorizedAccessException("You do not have access to this video.");
        }

        var settings = await _resolveRagSettings.Handle(cancellationToken);
        TranscriptHybridSearchComposer.EnsureHybridSearchEnabled(settings);

        var normalizedPage = TranscriptHybridSearchComposer.NormalizePage(page);
        var normalizedPageSize = TranscriptHybridSearchComposer.NormalizePageSize(pageSize);
        var normalizedLanguage = TranscriptHybridSearchComposer.NormalizeLanguage(language);
        var retrievalWindow = TranscriptHybridSearchComposer.CalculateRequestedWindow(normalizedPage, normalizedPageSize);

        var lexicalCandidateCount = TranscriptHybridSearchComposer.CalculateLexicalCandidateCount(settings, retrievalWindow, normalizedPageSize);
        var semanticCandidateCount = TranscriptHybridSearchComposer.CalculateSemanticCandidateCount(settings, retrievalWindow, normalizedPageSize);
        var mergedCandidateCount = TranscriptHybridSearchComposer.CalculateMergedCandidateCount(settings, retrievalWindow, normalizedPageSize);

        var lexicalTask = _unitOfWork.VideoTranscriptChunks.SearchLexicalByVideoAsync(
            videoId,
            query.Trim(),
            normalizedLanguage,
            1,
            lexicalCandidateCount,
            lexicalCandidateCount,
            cancellationToken);

        var semanticTask = _transcriptSearchProvider.SearchSemanticAsync(
            new TranscriptSemanticSearchRequest(
                query.Trim(),
                settings.EmbeddingProvider,
                settings.EmbeddingModel,
                normalizedLanguage,
                1,
                semanticCandidateCount,
                semanticCandidateCount,
                videoId,
                null),
            cancellationToken);

        await Task.WhenAll(lexicalTask, semanticTask);

        var merged = TranscriptHybridSearchComposer.Merge(
            lexicalTask.Result.Items,
            semanticTask.Result.Items,
            settings.HybridLexicalWeight,
            settings.HybridSemanticWeight,
            mergedCandidateCount);

        return TranscriptHybridSearchComposer.ToPerVideoResponse(merged, normalizedPage, normalizedPageSize);
    }
}

public sealed class SearchTranscriptHybridAcrossVideosService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ResolveRagSettingsService _resolveRagSettings;
    private readonly ITranscriptSearchProvider _transcriptSearchProvider;

    public SearchTranscriptHybridAcrossVideosService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ResolveRagSettingsService resolveRagSettings,
        ITranscriptSearchProvider transcriptSearchProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _resolveRagSettings = resolveRagSettings;
        _transcriptSearchProvider = transcriptSearchProvider;
    }

    public async Task<PagedResponseDto<CrossVideoTranscriptHybridSearchResultDto>> Handle(
        string query,
        string? language,
        IReadOnlyCollection<Guid>? videoIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("You must be authenticated to search across videos.");
        }

        var settings = await _resolveRagSettings.Handle(cancellationToken);
        TranscriptHybridSearchComposer.EnsureHybridSearchEnabled(settings);

        var normalizedPage = TranscriptHybridSearchComposer.NormalizePage(page);
        var normalizedPageSize = TranscriptHybridSearchComposer.NormalizePageSize(pageSize);
        var normalizedLanguage = TranscriptHybridSearchComposer.NormalizeLanguage(language);
        var retrievalWindow = TranscriptHybridSearchComposer.CalculateRequestedWindow(normalizedPage, normalizedPageSize);

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
            return new PagedResponseDto<CrossVideoTranscriptHybridSearchResultDto>(
                [],
                normalizedPage,
                normalizedPageSize,
                0,
                0,
                false,
                normalizedPage > 1);
        }

        var lexicalCandidateCount = TranscriptHybridSearchComposer.CalculateLexicalCandidateCount(settings, retrievalWindow, normalizedPageSize);
        var semanticCandidateCount = TranscriptHybridSearchComposer.CalculateSemanticCandidateCount(settings, retrievalWindow, normalizedPageSize);
        var mergedCandidateCount = TranscriptHybridSearchComposer.CalculateMergedCandidateCount(settings, retrievalWindow, normalizedPageSize);

        var lexicalTask = _unitOfWork.VideoTranscriptChunks.SearchLexicalAcrossVideosAsync(
            accessibleVideoIds,
            query.Trim(),
            normalizedLanguage,
            1,
            lexicalCandidateCount,
            lexicalCandidateCount,
            cancellationToken);

        var semanticTask = _transcriptSearchProvider.SearchSemanticAsync(
            new TranscriptSemanticSearchRequest(
                query.Trim(),
                settings.EmbeddingProvider,
                settings.EmbeddingModel,
                normalizedLanguage,
                1,
                semanticCandidateCount,
                semanticCandidateCount,
                null,
                accessibleVideoIds),
            cancellationToken);

        await Task.WhenAll(lexicalTask, semanticTask);

        var merged = TranscriptHybridSearchComposer.Merge(
            lexicalTask.Result.Items,
            semanticTask.Result.Items,
            settings.HybridLexicalWeight,
            settings.HybridSemanticWeight,
            mergedCandidateCount);

        return TranscriptHybridSearchComposer.ToCrossVideoResponse(merged, normalizedPage, normalizedPageSize);
    }
}

internal static class TranscriptHybridSearchComposer
{
    public static void EnsureHybridSearchEnabled(EffectiveRagSettings settings)
    {
        if (!settings.Enabled || !settings.SemanticSearchEnabled)
        {
            throw new ArgumentException("Hybrid transcript search is disabled.");
        }
    }

    public static int NormalizePage(int page) => page <= 0 ? 1 : page;

    public static int NormalizePageSize(int pageSize) => pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

    public static string? NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();

    public static int CalculateRequestedWindow(int page, int pageSize) => Math.Max(page * pageSize, pageSize);

    public static int CalculateLexicalCandidateCount(EffectiveRagSettings settings, int requestedWindow, int pageSize) =>
        Math.Max(Math.Max(settings.FullTextTopK, requestedWindow), pageSize);

    public static int CalculateSemanticCandidateCount(EffectiveRagSettings settings, int requestedWindow, int pageSize) =>
        Math.Max(Math.Max(settings.SemanticTopK, requestedWindow), pageSize);

    public static int CalculateMergedCandidateCount(EffectiveRagSettings settings, int requestedWindow, int pageSize) =>
        Math.Max(Math.Max(settings.HybridMaxCandidates, requestedWindow), pageSize);

    public static IReadOnlyList<HybridTranscriptChunkMatch> Merge(
        IReadOnlyCollection<TranscriptLexicalChunkMatch> lexicalMatches,
        IReadOnlyCollection<TranscriptSemanticChunkMatch> semanticMatches,
        double lexicalWeight,
        double semanticWeight,
        int maxCandidates)
    {
        var weights = NormalizeWeights(lexicalWeight, semanticWeight);
        var merged = new Dictionary<Guid, HybridTranscriptChunkAccumulator>();

        foreach (var lexicalMatch in lexicalMatches)
        {
            var accumulator = GetOrCreate(merged, lexicalMatch);
            accumulator.LexicalScore = lexicalMatch.Score;
            accumulator.VideoTitle ??= lexicalMatch.VideoTitle;
        }

        foreach (var semanticMatch in semanticMatches)
        {
            var accumulator = GetOrCreate(merged, semanticMatch);
            accumulator.SemanticScore = semanticMatch.Score;
            accumulator.VideoTitle ??= semanticMatch.VideoTitle;
        }

        return merged.Values
            .Select(item => item.ToMatch(weights.lexicalWeight, weights.semanticWeight))
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.SemanticScore ?? 0d)
            .ThenByDescending(item => item.LexicalScore ?? 0d)
            .ThenBy(item => item.StartSeconds)
            .ThenBy(item => item.ChunkId)
            .Take(maxCandidates)
            .ToArray();
    }

    public static PagedResponseDto<TranscriptHybridSearchResultDto> ToPerVideoResponse(
        IReadOnlyList<HybridTranscriptChunkMatch> merged,
        int page,
        int pageSize)
    {
        var paged = ApplyPaging(merged, page, pageSize);
        return new PagedResponseDto<TranscriptHybridSearchResultDto>(
            paged.items.Select(item => new TranscriptHybridSearchResultDto(
                    item.ChunkId,
                    item.VideoId,
                    item.TranscriptionId,
                    item.Language,
                    item.StartSeconds,
                    item.EndSeconds,
                    item.Content,
                    item.Score,
                    item.LexicalScore,
                    item.SemanticScore))
                .ToArray(),
            page,
            pageSize,
            paged.totalCount,
            paged.totalPages,
            paged.hasNextPage,
            paged.hasPreviousPage);
    }

    public static PagedResponseDto<CrossVideoTranscriptHybridSearchResultDto> ToCrossVideoResponse(
        IReadOnlyList<HybridTranscriptChunkMatch> merged,
        int page,
        int pageSize)
    {
        var paged = ApplyPaging(merged, page, pageSize);
        return new PagedResponseDto<CrossVideoTranscriptHybridSearchResultDto>(
            paged.items.Select(item => new CrossVideoTranscriptHybridSearchResultDto(
                    item.ChunkId,
                    item.VideoId,
                    item.VideoTitle ?? string.Empty,
                    item.TranscriptionId,
                    item.Language,
                    item.StartSeconds,
                    item.EndSeconds,
                    item.Content,
                    item.Score,
                    item.LexicalScore,
                    item.SemanticScore))
                .ToArray(),
            page,
            pageSize,
            paged.totalCount,
            paged.totalPages,
            paged.hasNextPage,
            paged.hasPreviousPage);
    }

    private static (IReadOnlyList<HybridTranscriptChunkMatch> items, int totalCount, int totalPages, bool hasNextPage, bool hasPreviousPage) ApplyPaging(
        IReadOnlyList<HybridTranscriptChunkMatch> merged,
        int page,
        int pageSize)
    {
        var totalCount = merged.Count;
        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
        var items = merged
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return (items, totalCount, totalPages, page < totalPages, page > 1 && totalPages > 0);
    }

    private static (double lexicalWeight, double semanticWeight) NormalizeWeights(double lexicalWeight, double semanticWeight)
    {
        lexicalWeight = Math.Max(0d, lexicalWeight);
        semanticWeight = Math.Max(0d, semanticWeight);

        var weightSum = lexicalWeight + semanticWeight;
        if (weightSum <= 0d)
        {
            return (0.5d, 0.5d);
        }

        return (lexicalWeight / weightSum, semanticWeight / weightSum);
    }

    private static HybridTranscriptChunkAccumulator GetOrCreate(
        IDictionary<Guid, HybridTranscriptChunkAccumulator> merged,
        TranscriptLexicalChunkMatch match)
    {
        if (merged.TryGetValue(match.ChunkId, out var existing))
        {
            return existing;
        }

        var created = new HybridTranscriptChunkAccumulator(
            match.ChunkId,
            match.VideoId,
            match.TranscriptionId,
            match.Language,
            match.StartSeconds,
            match.EndSeconds,
            match.Content,
            match.VideoTitle);

        merged.Add(match.ChunkId, created);
        return created;
    }

    private static HybridTranscriptChunkAccumulator GetOrCreate(
        IDictionary<Guid, HybridTranscriptChunkAccumulator> merged,
        TranscriptSemanticChunkMatch match)
    {
        if (merged.TryGetValue(match.ChunkId, out var existing))
        {
            return existing;
        }

        var created = new HybridTranscriptChunkAccumulator(
            match.ChunkId,
            match.VideoId,
            match.TranscriptionId,
            match.Language,
            match.StartSeconds,
            match.EndSeconds,
            match.Content,
            match.VideoTitle);

        merged.Add(match.ChunkId, created);
        return created;
    }

    private sealed class HybridTranscriptChunkAccumulator
    {
        public HybridTranscriptChunkAccumulator(
            Guid chunkId,
            Guid videoId,
            Guid transcriptionId,
            string language,
            double startSeconds,
            double endSeconds,
            string content,
            string? videoTitle)
        {
            ChunkId = chunkId;
            VideoId = videoId;
            TranscriptionId = transcriptionId;
            Language = language;
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Content = content;
            VideoTitle = videoTitle;
        }

        public Guid ChunkId { get; }

        public Guid VideoId { get; }

        public Guid TranscriptionId { get; }

        public string Language { get; }

        public double StartSeconds { get; }

        public double EndSeconds { get; }

        public string Content { get; }

        public string? VideoTitle { get; set; }

        public double? LexicalScore { get; set; }

        public double? SemanticScore { get; set; }

        public HybridTranscriptChunkMatch ToMatch(double lexicalWeight, double semanticWeight)
        {
            var score = ((LexicalScore ?? 0d) * lexicalWeight) + ((SemanticScore ?? 0d) * semanticWeight);

            return new HybridTranscriptChunkMatch(
                ChunkId,
                VideoId,
                TranscriptionId,
                Language,
                StartSeconds,
                EndSeconds,
                Content,
                Math.Clamp(score, 0d, 1d),
                LexicalScore,
                SemanticScore,
                VideoTitle);
        }
    }
}

internal sealed record HybridTranscriptChunkMatch(
    Guid ChunkId,
    Guid VideoId,
    Guid TranscriptionId,
    string Language,
    double StartSeconds,
    double EndSeconds,
    string Content,
    double Score,
    double? LexicalScore,
    double? SemanticScore,
    string? VideoTitle);
