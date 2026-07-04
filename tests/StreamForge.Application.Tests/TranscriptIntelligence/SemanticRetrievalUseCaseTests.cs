using FluentAssertions;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.TranscriptIntelligence;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.TranscriptIntelligence;

public sealed class SemanticRetrievalUseCaseTests
{
    private static readonly RagOptions EnabledDefaults = new()
    {
        Enabled = true,
        SemanticSearchEnabled = true,
        EmbeddingProvider = "local-sentence-transformer",
        EmbeddingModel = "sentence-transformers/all-MiniLM-L6-v2",
        SemanticTopK = 8,
        FullTextTopK = 8,
        HybridSemanticWeight = 0.6d,
        HybridLexicalWeight = 0.4d,
        HybridMaxCandidates = 12,
        QaProvider = "gemini",
        QaMaxContextChunks = 8,
        QaMaxCitations = 5,
        QaTemperature = 0d,
        QaMaxOutputTokens = 512,
        QaProviderConfigs = new RagQaProviderConfigs
        {
            Gemini = new RagGeminiQaOptions
            {
                Model = "gemini-2.5-flash"
            }
        }
    };

    [Fact]
    public async Task SearchVideoTranscriptSemantic_ShouldAuthorizeWithShareTokenAndReturnPagedResults()
    {
        var videoId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        currentUser.Role.Returns((UserRole?)null);
        currentUser.IsAuthenticated.Returns(false);

        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanViewVideoAsync(videoId, null, null, "share-123", Arg.Any<CancellationToken>())
            .Returns(true);

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        searchProvider.SearchSemanticAsync(Arg.Any<TranscriptSemanticSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptSemanticChunkMatch>(
            [
                new TranscriptSemanticChunkMatch(
                    Guid.NewGuid(),
                    videoId,
                    Guid.NewGuid(),
                    "en",
                    5,
                    10,
                    "semantic hit",
                    0.91d,
                    null)
            ],
            1,
            1,
            20));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());

        var service = new SearchVideoTranscriptSemanticService(
            currentUser,
            authorization,
            new ResolveRagSettingsService(unitOfWork, EnabledDefaults),
            searchProvider);

        var result = await service.Handle(videoId, "meaning", "en", 0, 999, "share-123", CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Content.Should().Be("semantic hit");
        result.Items[0].Score.Should().Be(0.91d);
        await authorization.Received(1).CanViewVideoAsync(videoId, null, null, "share-123", Arg.Any<CancellationToken>());
        await searchProvider.Received(1).SearchSemanticAsync(
            Arg.Is<TranscriptSemanticSearchRequest>(request =>
                request.VideoId == videoId &&
                request.VideoIds == null &&
                request.Language == "en" &&
                request.Page == 1 &&
                request.PageSize == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchVideoTranscriptSemantic_ShouldRejectWhenSemanticSearchIsDisabled()
    {
        var videoId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Viewer);
        currentUser.IsAuthenticated.Returns(true);

        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanViewVideoAsync(videoId, currentUser.UserId, currentUser.Role, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());

        var service = new SearchVideoTranscriptSemanticService(
            currentUser,
            authorization,
            new ResolveRagSettingsService(unitOfWork, new RagOptions { Enabled = true, SemanticSearchEnabled = false }),
            Substitute.For<ITranscriptSearchProvider>());

        var act = () => service.Handle(videoId, "meaning", null, 1, 20, null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*disabled*");
    }

    [Fact]
    public async Task SearchTranscriptSemanticAcrossVideos_ShouldIntersectRequestedScopeWithAccessibleVideos()
    {
        var allowedVideoId = Guid.NewGuid();
        var deniedVideoId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.Role.Returns(UserRole.Editor);
        currentUser.IsAuthenticated.Returns(true);

        var videosRepo = Substitute.For<IVideoRepository>();
        videosRepo.GetAccessibleVideoIdsAsync(
                userId,
                UserRole.Editor,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([allowedVideoId]);

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        searchProvider.SearchSemanticAsync(Arg.Any<TranscriptSemanticSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptSemanticChunkMatch>(
            [
                new TranscriptSemanticChunkMatch(
                    Guid.NewGuid(),
                    allowedVideoId,
                    Guid.NewGuid(),
                    "en",
                    2,
                    6,
                    "cross video hit",
                    0.87d,
                    "Allowed Video")
            ],
            1,
            1,
            20));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        unitOfWork.Videos.Returns(videosRepo);

        var service = new SearchTranscriptSemanticAcrossVideosService(
            unitOfWork,
            currentUser,
            new ResolveRagSettingsService(unitOfWork, EnabledDefaults),
            searchProvider);

        var result = await service.Handle(
            "meaning",
            "en",
            [allowedVideoId, deniedVideoId],
            1,
            20,
            CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].VideoId.Should().Be(allowedVideoId);
        result.Items[0].VideoTitle.Should().Be("Allowed Video");
        await searchProvider.Received(1).SearchSemanticAsync(
            Arg.Is<TranscriptSemanticSearchRequest>(request =>
                request.VideoId == null &&
                request.VideoIds != null &&
                request.VideoIds.Count == 1 &&
                request.VideoIds.Contains(allowedVideoId) &&
                !request.VideoIds.Contains(deniedVideoId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchTranscriptSemanticAcrossVideos_ShouldReturnEmptyPageWhenNoAccessibleVideosRemain()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Viewer);
        currentUser.IsAuthenticated.Returns(true);

        var videosRepo = Substitute.For<IVideoRepository>();
        videosRepo.GetAccessibleVideoIdsAsync(
                currentUser.UserId,
                currentUser.Role,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        unitOfWork.Videos.Returns(videosRepo);

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        var service = new SearchTranscriptSemanticAcrossVideosService(
            unitOfWork,
            currentUser,
            new ResolveRagSettingsService(unitOfWork, EnabledDefaults),
            searchProvider);

        var result = await service.Handle("meaning", null, null, 2, 10, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(0);
        await searchProvider.DidNotReceiveWithAnyArgs().SearchSemanticAsync(default!, default);
    }

    [Fact]
    public async Task SearchVideoTranscriptHybrid_ShouldMergeLexicalAndSemanticMatchesByChunk()
    {
        var videoId = Guid.NewGuid();
        var mergedChunkId = Guid.NewGuid();
        var lexicalOnlyChunkId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Viewer);
        currentUser.IsAuthenticated.Returns(true);

        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanViewVideoAsync(videoId, currentUser.UserId, currentUser.Role, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var chunkRepository = Substitute.For<IVideoTranscriptChunkRepository>();
        chunkRepository.SearchLexicalByVideoAsync(
                videoId,
                "meaning",
                "en",
                1,
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptLexicalChunkMatch>(
            [
                new TranscriptLexicalChunkMatch(
                    mergedChunkId,
                    videoId,
                    Guid.NewGuid(),
                    "en",
                    5,
                    9,
                    "merged hit",
                    0.8d,
                    null),
                new TranscriptLexicalChunkMatch(
                    lexicalOnlyChunkId,
                    videoId,
                    Guid.NewGuid(),
                    "en",
                    15,
                    19,
                    "lexical only",
                    0.7d,
                    null)
            ],
            2,
            1,
            20));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        unitOfWork.VideoTranscriptChunks.Returns(chunkRepository);

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        searchProvider.SearchSemanticAsync(Arg.Any<TranscriptSemanticSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptSemanticChunkMatch>(
            [
                new TranscriptSemanticChunkMatch(
                    mergedChunkId,
                    videoId,
                    Guid.NewGuid(),
                    "en",
                    5,
                    9,
                    "merged hit",
                    0.9d,
                    null)
            ],
            1,
            1,
            20));

        var service = new SearchVideoTranscriptHybridService(
            unitOfWork,
            currentUser,
            authorization,
            new ResolveRagSettingsService(unitOfWork, EnabledDefaults),
            searchProvider);

        var result = await service.Handle(videoId, "meaning", "en", 1, 20, null, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].ChunkId.Should().Be(mergedChunkId);
        result.Items[0].LexicalScore.Should().Be(0.8d);
        result.Items[0].SemanticScore.Should().Be(0.9d);
        result.Items[0].Score.Should().BeApproximately(0.86d, 0.0001d);
        result.Items[1].ChunkId.Should().Be(lexicalOnlyChunkId);
        result.Items[1].SemanticScore.Should().BeNull();
    }

    [Fact]
    public async Task SearchTranscriptHybridAcrossVideos_ShouldIntersectRequestedScopeWithAccessibleVideos()
    {
        var allowedVideoId = Guid.NewGuid();
        var deniedVideoId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.Role.Returns(UserRole.Editor);
        currentUser.IsAuthenticated.Returns(true);

        var videosRepo = Substitute.For<IVideoRepository>();
        videosRepo.GetAccessibleVideoIdsAsync(
                userId,
                UserRole.Editor,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([allowedVideoId]);

        var chunkRepository = Substitute.For<IVideoTranscriptChunkRepository>();
        chunkRepository.SearchLexicalAcrossVideosAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                "meaning",
                "en",
                1,
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptLexicalChunkMatch>(
            [
                new TranscriptLexicalChunkMatch(
                    Guid.NewGuid(),
                    allowedVideoId,
                    Guid.NewGuid(),
                    "en",
                    2,
                    6,
                    "cross lexical hit",
                    0.6d,
                    "Allowed Video")
            ],
            1,
            1,
            20));

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        searchProvider.SearchSemanticAsync(Arg.Any<TranscriptSemanticSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptSemanticChunkMatch>(
            [
                new TranscriptSemanticChunkMatch(
                    Guid.NewGuid(),
                    allowedVideoId,
                    Guid.NewGuid(),
                    "en",
                    10,
                    14,
                    "cross semantic hit",
                    0.85d,
                    "Allowed Video")
            ],
            1,
            1,
            20));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        unitOfWork.Videos.Returns(videosRepo);
        unitOfWork.VideoTranscriptChunks.Returns(chunkRepository);

        var service = new SearchTranscriptHybridAcrossVideosService(
            unitOfWork,
            currentUser,
            new ResolveRagSettingsService(unitOfWork, EnabledDefaults),
            searchProvider);

        var result = await service.Handle(
            "meaning",
            "en",
            [allowedVideoId, deniedVideoId],
            1,
            20,
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(item => item.VideoId == allowedVideoId);
        result.Items.Should().OnlyContain(item => item.VideoTitle == "Allowed Video");

        await chunkRepository.Received(1).SearchLexicalAcrossVideosAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(allowedVideoId) && !ids.Contains(deniedVideoId)),
            "meaning",
            "en",
            1,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());

        await searchProvider.Received(1).SearchSemanticAsync(
            Arg.Is<TranscriptSemanticSearchRequest>(request =>
                request.VideoIds != null &&
                request.VideoIds.Count == 1 &&
                request.VideoIds.Contains(allowedVideoId) &&
                !request.VideoIds.Contains(deniedVideoId)),
            Arg.Any<CancellationToken>());
    }
}
