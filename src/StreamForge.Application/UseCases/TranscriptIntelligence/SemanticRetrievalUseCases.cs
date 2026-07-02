using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.TranscriptIntelligence;

public sealed class SearchVideoTranscriptSemanticService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ResolveRagSettingsService _resolveRagSettings;
    private readonly ITranscriptSearchProvider _transcriptSearchProvider;

    public SearchVideoTranscriptSemanticService(
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ResolveRagSettingsService resolveRagSettings,
        ITranscriptSearchProvider transcriptSearchProvider)
    {
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _resolveRagSettings = resolveRagSettings;
        _transcriptSearchProvider = transcriptSearchProvider;
    }

    public async Task<PagedResponseDto<TranscriptSemanticSearchResultDto>> Handle(
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
        EnsureSemanticSearchEnabled(settings);

        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var candidateCount = NormalizeCandidateCount(
            settings.SemanticTopK,
            normalizedPage,
            normalizedPageSize);

        var result = await _transcriptSearchProvider.SearchSemanticAsync(
            new TranscriptSemanticSearchRequest(
                query.Trim(),
                settings.EmbeddingProvider,
                settings.EmbeddingModel,
                NormalizeLanguage(language),
                normalizedPage,
                normalizedPageSize,
                candidateCount,
                videoId,
                null),
            cancellationToken);

        return ToPagedResponse(
            result,
            item => new TranscriptSemanticSearchResultDto(
                item.ChunkId,
                item.VideoId,
                item.TranscriptionId,
                item.Language,
                item.StartSeconds,
                item.EndSeconds,
                item.Content,
                item.Score));
    }

    private static void EnsureSemanticSearchEnabled(EffectiveRagSettings settings)
    {
        if (!settings.Enabled || !settings.SemanticSearchEnabled)
        {
            throw new ArgumentException("Semantic transcript search is disabled.");
        }
    }

    private static int NormalizePage(int page) => page <= 0 ? 1 : page;

    private static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

    private static int NormalizeCandidateCount(int semanticTopK, int page, int pageSize) =>
        Math.Max(Math.Max(semanticTopK, page * pageSize), pageSize);

    private static string? NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();

    internal static PagedResponseDto<TDestination> ToPagedResponse<TDestination>(
        PagedQueryResult<TranscriptSemanticChunkMatch> result,
        Func<TranscriptSemanticChunkMatch, TDestination> map)
    {
        var totalPages = result.PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)result.TotalCount / result.PageSize);

        return new PagedResponseDto<TDestination>(
            result.Items.Select(map).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            totalPages,
            result.Page < totalPages,
            result.Page > 1 && totalPages > 0);
    }
}

public sealed class SearchTranscriptSemanticAcrossVideosService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ResolveRagSettingsService _resolveRagSettings;
    private readonly ITranscriptSearchProvider _transcriptSearchProvider;

    public SearchTranscriptSemanticAcrossVideosService(
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

    public async Task<PagedResponseDto<CrossVideoTranscriptSemanticSearchResultDto>> Handle(
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
        EnsureSemanticSearchEnabled(settings);

        var scopedVideoIds = videoIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var accessibleVideoIds = await _unitOfWork.Videos.GetAccessibleVideoIdsAsync(
            _currentUserService.UserId,
            _currentUserService.Role,
            scopedVideoIds,
            cancellationToken);

        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        if (accessibleVideoIds.Count == 0)
        {
            return new PagedResponseDto<CrossVideoTranscriptSemanticSearchResultDto>(
                [],
                normalizedPage,
                normalizedPageSize,
                0,
                0,
                false,
                normalizedPage > 1);
        }

        var candidateCount = NormalizeCandidateCount(
            settings.SemanticTopK,
            normalizedPage,
            normalizedPageSize);

        var result = await _transcriptSearchProvider.SearchSemanticAsync(
            new TranscriptSemanticSearchRequest(
                query.Trim(),
                settings.EmbeddingProvider,
                settings.EmbeddingModel,
                NormalizeLanguage(language),
                normalizedPage,
                normalizedPageSize,
                candidateCount,
                null,
                accessibleVideoIds),
            cancellationToken);

        return SearchVideoTranscriptSemanticService.ToPagedResponse(
            result,
            item => new CrossVideoTranscriptSemanticSearchResultDto(
                item.ChunkId,
                item.VideoId,
                item.VideoTitle ?? string.Empty,
                item.TranscriptionId,
                item.Language,
                item.StartSeconds,
                item.EndSeconds,
                item.Content,
                item.Score));
    }

    private static void EnsureSemanticSearchEnabled(EffectiveRagSettings settings)
    {
        if (!settings.Enabled || !settings.SemanticSearchEnabled)
        {
            throw new ArgumentException("Semantic transcript search is disabled.");
        }
    }

    private static int NormalizePage(int page) => page <= 0 ? 1 : page;

    private static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

    private static int NormalizeCandidateCount(int semanticTopK, int page, int pageSize) =>
        Math.Max(Math.Max(semanticTopK, page * pageSize), pageSize);

    private static string? NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant();
}
