using FluentAssertions;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.TranscriptIntelligence;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.TranscriptIntelligence;

public sealed class QuestionAnsweringUseCaseTests
{
    private static readonly RagOptions _enabledDefaults = new()
    {
        Enabled = true,
        SemanticSearchEnabled = true,
        VideoQuestionsEnabled = true,
        CrossVideoQuestionsEnabled = true,
        EmbeddingProvider = "local-sentence-transformer",
        EmbeddingModel = "sentence-transformers/all-MiniLM-L6-v2",
        SemanticTopK = 8,
        FullTextTopK = 8,
        HybridSemanticWeight = 0.6d,
        HybridLexicalWeight = 0.4d,
        HybridMaxCandidates = 12,
        QaProvider = "gemini",
        QaMaxContextChunks = 4,
        QaMaxCitations = 3,
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
    public async Task AskVideoQuestion_ShouldAuthorizeWithShareTokenAndReturnGroundedCitations()
    {
        var videoId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var transcriptionId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        currentUser.Role.Returns((UserRole?)null);
        currentUser.IsAuthenticated.Returns(false);

        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanViewVideoAsync(videoId, null, null, "share-123", Arg.Any<CancellationToken>())
            .Returns(true);

        var chunkRepository = Substitute.For<IVideoTranscriptChunkRepository>();
        chunkRepository.SearchLexicalByVideoAsync(
                videoId,
                "what happened?",
                "en",
                1,
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptLexicalChunkMatch>(
            [
                new TranscriptLexicalChunkMatch(
                    chunkId,
                    videoId,
                    transcriptionId,
                    "en",
                    5,
                    10,
                    "The key event happened here.",
                    0.8d,
                    null)
            ],
            1,
            1,
            10));

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        searchProvider.SearchSemanticAsync(Arg.Any<TranscriptSemanticSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptSemanticChunkMatch>(
            [
                new TranscriptSemanticChunkMatch(
                    chunkId,
                    videoId,
                    transcriptionId,
                    "en",
                    5,
                    10,
                    "The key event happened here.",
                    0.9d,
                    null)
            ],
            1,
            1,
            10));

        var videosRepo = Substitute.For<IVideoRepository>();
        videosRepo.GetByIdAsync(videoId, Arg.Any<CancellationToken>())
            .Returns(Video.Create("Test Video", null, Guid.NewGuid()));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        unitOfWork.VideoTranscriptChunks.Returns(chunkRepository);
        unitOfWork.Videos.Returns(videosRepo);

        var qaProvider = Substitute.For<IVideoQuestionAnsweringProvider>();
        qaProvider.AnswerAsync(Arg.Any<GroundedQuestionAnsweringRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GroundedQuestionAnsweringResult(
                true,
                "The key event happened around 5 seconds.",
                [chunkId],
                "gemini",
                "gemini-2.5-flash",
                "{\"canAnswer\":true}"));

        var factory = Substitute.For<IVideoQuestionAnsweringProviderFactory>();
        factory.Resolve("gemini").Returns(qaProvider);

        var service = new AskVideoQuestionService(
            unitOfWork,
            currentUser,
            authorization,
            new ResolveRagSettingsService(unitOfWork, _enabledDefaults),
            searchProvider,
            factory);

        var result = await service.Handle(videoId, "what happened?", "en", "share-123", CancellationToken.None);

        result.Answer.Should().Contain("5 seconds");
        result.Citations.Should().ContainSingle();
        result.Citations[0].ChunkId.Should().Be(chunkId);
        result.Citations[0].VideoTitle.Should().Be("Test Video");
        result.RetrievalMode.Should().Be("hybrid");
        result.UsedChunkCount.Should().Be(1);
        await authorization.Received(1).CanViewVideoAsync(videoId, null, null, "share-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AskQuestionAcrossVideos_ShouldIntersectRequestedScopeWithAccessibleVideos()
    {
        var allowedVideoId = Guid.NewGuid();
        var deniedVideoId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var transcriptionId = Guid.NewGuid();
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
                "summarize",
                "en",
                1,
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptLexicalChunkMatch>(
            [
                new TranscriptLexicalChunkMatch(
                    chunkId,
                    allowedVideoId,
                    transcriptionId,
                    "en",
                    12,
                    18,
                    "Important cross-video evidence.",
                    0.7d,
                    "Allowed Video")
            ],
            1,
            1,
            10));

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        searchProvider.SearchSemanticAsync(Arg.Any<TranscriptSemanticSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptSemanticChunkMatch>(
            [
                new TranscriptSemanticChunkMatch(
                    chunkId,
                    allowedVideoId,
                    transcriptionId,
                    "en",
                    12,
                    18,
                    "Important cross-video evidence.",
                    0.92d,
                    "Allowed Video")
            ],
            1,
            1,
            10));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        unitOfWork.Videos.Returns(videosRepo);
        unitOfWork.VideoTranscriptChunks.Returns(chunkRepository);

        var qaProvider = Substitute.For<IVideoQuestionAnsweringProvider>();
        qaProvider.AnswerAsync(Arg.Any<GroundedQuestionAnsweringRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GroundedQuestionAnsweringResult(
                true,
                "Here is the grounded summary.",
                [chunkId],
                "gemini",
                "gemini-2.5-flash",
                "{\"canAnswer\":true}"));

        var factory = Substitute.For<IVideoQuestionAnsweringProviderFactory>();
        factory.Resolve("gemini").Returns(qaProvider);

        var service = new AskQuestionAcrossVideosService(
            unitOfWork,
            currentUser,
            new ResolveRagSettingsService(unitOfWork, _enabledDefaults),
            searchProvider,
            factory);

        var result = await service.Handle("summarize", "en", [allowedVideoId, deniedVideoId], CancellationToken.None);

        result.Citations.Should().ContainSingle();
        result.Citations[0].VideoId.Should().Be(allowedVideoId);
        result.Citations[0].VideoTitle.Should().Be("Allowed Video");

        await chunkRepository.Received(1).SearchLexicalAcrossVideosAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(allowedVideoId) && !ids.Contains(deniedVideoId)),
            "summarize",
            "en",
            1,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AskVideoQuestion_ShouldReturnNoAnswerWhenNoEvidenceIsRetrieved()
    {
        var videoId = Guid.NewGuid();

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
                "what happened?",
                null,
                1,
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptLexicalChunkMatch>([], 0, 1, 10));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        unitOfWork.VideoTranscriptChunks.Returns(chunkRepository);
        unitOfWork.Videos.Returns(Substitute.For<IVideoRepository>());
        unitOfWork.Videos.GetByIdAsync(videoId, Arg.Any<CancellationToken>())
            .Returns(Video.Create("Test Video", null, Guid.NewGuid()));

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        searchProvider.SearchSemanticAsync(Arg.Any<TranscriptSemanticSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptSemanticChunkMatch>([], 0, 1, 10));

        var qaProvider = Substitute.For<IVideoQuestionAnsweringProvider>();
        var factory = Substitute.For<IVideoQuestionAnsweringProviderFactory>();
        factory.Resolve("gemini").Returns(qaProvider);

        var service = new AskVideoQuestionService(
            unitOfWork,
            currentUser,
            authorization,
            new ResolveRagSettingsService(unitOfWork, _enabledDefaults),
            searchProvider,
            factory);

        var result = await service.Handle(videoId, "what happened?", null, null, CancellationToken.None);

        result.Answer.Should().Contain("couldn't find enough transcript evidence");
        result.Citations.Should().BeEmpty();
        result.UsedChunkCount.Should().Be(0);
        await qaProvider.DidNotReceiveWithAnyArgs().AnswerAsync(default!, default);
    }

    [Fact]
    public async Task AskVideoQuestion_ShouldRejectProviderCitationIdsOutsideEvidenceSet()
    {
        var videoId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var unknownChunkId = Guid.NewGuid();
        var transcriptionId = Guid.NewGuid();

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
                "what happened?",
                null,
                1,
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptLexicalChunkMatch>(
            [
                new TranscriptLexicalChunkMatch(
                    chunkId,
                    videoId,
                    transcriptionId,
                    "en",
                    5,
                    10,
                    "The key event happened here.",
                    0.8d,
                    null)
            ],
            1,
            1,
            10));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        unitOfWork.VideoTranscriptChunks.Returns(chunkRepository);
        unitOfWork.Videos.Returns(Substitute.For<IVideoRepository>());
        unitOfWork.Videos.GetByIdAsync(videoId, Arg.Any<CancellationToken>())
            .Returns(Video.Create("Test Video", null, Guid.NewGuid()));

        var searchProvider = Substitute.For<ITranscriptSearchProvider>();
        searchProvider.SearchSemanticAsync(Arg.Any<TranscriptSemanticSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<TranscriptSemanticChunkMatch>([], 0, 1, 10));

        var qaProvider = Substitute.For<IVideoQuestionAnsweringProvider>();
        qaProvider.AnswerAsync(Arg.Any<GroundedQuestionAnsweringRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GroundedQuestionAnsweringResult(
                true,
                "An ungrounded answer.",
                [unknownChunkId],
                "gemini",
                "gemini-2.5-flash",
                "{\"canAnswer\":true}"));

        var factory = Substitute.For<IVideoQuestionAnsweringProviderFactory>();
        factory.Resolve("gemini").Returns(qaProvider);

        var service = new AskVideoQuestionService(
            unitOfWork,
            currentUser,
            authorization,
            new ResolveRagSettingsService(unitOfWork, _enabledDefaults),
            searchProvider,
            factory);

        var act = () => service.Handle(videoId, "what happened?", null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*outside the grounded evidence set*");
    }

    [Fact]
    public async Task AskVideoQuestion_ShouldRejectWhenVideoQuestionsAreDisabled()
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

        var service = new AskVideoQuestionService(
            unitOfWork,
            currentUser,
            authorization,
            new ResolveRagSettingsService(unitOfWork, new RagOptions
            {
                Enabled = true,
                SemanticSearchEnabled = true,
                VideoQuestionsEnabled = false
            }),
            Substitute.For<ITranscriptSearchProvider>(),
            Substitute.For<IVideoQuestionAnsweringProviderFactory>());

        var act = () => service.Handle(videoId, "what happened?", null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*disabled*");
    }
}
