using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Transcriptions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;
using System.Reflection;

namespace StreamForge.Application.Tests.Transcriptions;

public sealed class TranscriptionUseCaseTests
{
    [Fact]
    public async Task ListVideoTranscriptions_ShouldSelfHealOrphanedProcessingRows()
    {
        var videoId = Guid.NewGuid();
        var transcription = VideoTranscription.Create(
            videoId,
            "auto",
            "vtt",
            @"videos\video\transcriptions\auto\captions.vtt",
            "local-faster-whisper");
        ForceTranscriptionState(transcription, TranscriptionStatus.Processing, workerJobId: null, correlationId: null);

        var transcriptionsRepo = Substitute.For<IVideoTranscriptionRepository>();
        transcriptionsRepo.GetByVideoIdAsync(videoId, Arg.Any<CancellationToken>())
            .Returns([transcription]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoTranscriptions.Returns(transcriptionsRepo);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Viewer);
        currentUser.IsAuthenticated.Returns(true);

        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanViewVideoAsync(videoId, currentUser.UserId, currentUser.Role, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var reconciler = CreateReconciler(unitOfWork, Substitute.For<ITranscriptionProvider>());
        var service = new ListVideoTranscriptionsService(unitOfWork, currentUser, authorization, Substitute.For<ITranscriptionProvider>(), reconciler);

        var result = await service.Handle(videoId, null, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Status.Should().Be("Failed");
        result[0].FailureReason.Should().Contain("orphaned");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartVideoTranscription_ShouldIgnoreOrphanedActiveRowsAndSubmitNewJob()
    {
        var videoId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();
        var video = Video.Create("Video", null, uploaderId, status: VideoStatus.Ready);
        SetPrivateProperty(video, nameof(Video.Id), videoId);

        var orphan = VideoTranscription.Create(
            videoId,
            "auto",
            "vtt",
            @"videos\video\transcriptions\auto\captions.vtt",
            "local-faster-whisper");
        ForceTranscriptionState(orphan, TranscriptionStatus.Processing, workerJobId: null, correlationId: null);

        var newRows = new List<VideoTranscription>();

        var transcriptionsRepo = Substitute.For<IVideoTranscriptionRepository>();
        transcriptionsRepo.GetByVideoAndStatusAsync(videoId, TranscriptionStatus.Pending, TranscriptionStatus.Processing)
            .Returns(
                Task.FromResult<IReadOnlyList<VideoTranscription>>([orphan]),
                Task.FromResult<IReadOnlyList<VideoTranscription>>([]));
        transcriptionsRepo.GetByVideoLanguageAndFormatAsync(videoId, "en", "vtt", Arg.Any<CancellationToken>())
            .Returns((VideoTranscription?)null);
        transcriptionsRepo.AddAsync(Arg.Any<VideoTranscription>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var row = call.Arg<VideoTranscription>();
                newRows.Add(row);
                return Task.FromResult(row);
            });

        var videosRepo = Substitute.For<IVideoRepository>();
        videosRepo.GetByIdAsync(videoId, Arg.Any<CancellationToken>())
            .Returns(video);

        var filesRepo = Substitute.For<IVideoFileRepository>();
        filesRepo.GetOriginalByVideoIdAsync(videoId, Arg.Any<CancellationToken>())
            .Returns(VideoFile.Create(Guid.NewGuid(), Guid.NewGuid(), "videos/source.mp4", 100, "video/mp4"));

        var systemSettings = Substitute.For<ISystemSettingRepository>();
        systemSettings.GetByKeysAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoTranscriptions.Returns(transcriptionsRepo);
        unitOfWork.Videos.Returns(videosRepo);
        unitOfWork.VideoFiles.Returns(filesRepo);
        unitOfWork.SystemSettings.Returns(systemSettings);

        var provider = Substitute.For<ITranscriptionProvider>();
        provider.SubmitAsync(Arg.Any<TranscriptionProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TranscriptionSubmissionResult("worker-123", "accepted"));

        var options = new TranscriptionOptions
        {
            Enabled = true,
            Provider = "local-faster-whisper",
            CallbackBaseUrl = "http://127.0.0.1:5186"
        };

        var reconciler = CreateReconciler(unitOfWork, provider);
        var service = new StartVideoTranscriptionService(
            unitOfWork,
            provider,
            options,
            new ResolveTranscriptionSettingsService(unitOfWork, options),
            reconciler,
            Substitute.For<ILogger<StartVideoTranscriptionService>>());

        var result = await service.Handle(videoId, "en", ["vtt"], CancellationToken.None);

        orphan.Status.Should().Be(TranscriptionStatus.Failed);
        newRows.Should().ContainSingle();
        result.Should().ContainSingle();
        result[0].Language.Should().Be("en");
        result[0].WorkerJobId.Should().Be("worker-123");
    }

    [Fact]
    public async Task ResolveTranscriptionSettings_ShouldPreferSystemSettingsOverDefaults()
    {
        var defaults = new TranscriptionOptions
        {
            Enabled = false,
            AutoTranscribeOnReady = false,
            Provider = "local-faster-whisper",
            DefaultLanguage = "auto",
            OutputFormats = ["vtt"],
            LocalFasterWhisper = new LocalFasterWhisperOptions
            {
                Model = "small",
                Device = "cpu",
                ComputeType = "int8",
                BeamSize = 5,
                EnableVad = false,
                EnableWordTimestamps = false
            }
        };

        var settingsRepo = Substitute.For<ISystemSettingRepository>();
        settingsRepo.GetByKeysAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                SystemSetting.Create("transcription.enabled", "true"),
                SystemSetting.Create("transcription.autoTranscribeOnReady", "true"),
                SystemSetting.Create("transcription.defaultLanguage", "en"),
                SystemSetting.Create("transcription.outputFormats", JsonSerializer.Serialize(new[] { "vtt", "srt" })),
                SystemSetting.Create("transcription.localFasterWhisper.model", "medium"),
                SystemSetting.Create("transcription.localFasterWhisper.beamSize", "7")
            ]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(settingsRepo);

        var service = new ResolveTranscriptionSettingsService(unitOfWork, defaults);

        var result = await service.Handle(CancellationToken.None);

        result.Enabled.Should().BeTrue();
        result.AutoTranscribeOnReady.Should().BeTrue();
        result.Provider.Should().Be("local-faster-whisper");
        result.DefaultLanguage.Should().Be("en");
        result.OutputFormats.Should().BeEquivalentTo(["vtt", "srt"]);
        result.Model.Should().Be("medium");
        result.BeamSize.Should().Be(7);
        result.Device.Should().Be("cpu");
        result.ComputeType.Should().Be("int8");
    }

    [Fact]
    public async Task UpdateAdminTranscriptionSettings_ShouldUpsertSettingsAndPersist()
    {
        var settingsRepo = Substitute.For<ISystemSettingRepository>();
        settingsRepo.GetByKeyAsync("transcription.enabled", Arg.Any<CancellationToken>())
            .Returns(SystemSetting.Create("transcription.enabled", "false"));

        var addedSettings = new List<SystemSetting>();
        settingsRepo.AddAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var setting = call.Arg<SystemSetting>();
                addedSettings.Add(setting);
                return Task.FromResult(setting);
            });

        var updatedSettings = new List<SystemSetting>();
        settingsRepo.UpdateAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                updatedSettings.Add(call.Arg<SystemSetting>());
                return Task.CompletedTask;
            });

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SystemSettings.Returns(settingsRepo);

        var service = new UpdateAdminTranscriptionSettingsService(unitOfWork);
        var request = new UpdateAdminTranscriptionSettingsRequestDto(
            true,
            true,
            "local-faster-whisper",
            "en",
            ["vtt", "srt", "vtt"],
            "medium",
            "cpu",
            "int8",
            8,
            true,
            false);

        var result = await service.Handle(request, CancellationToken.None);

        result.Enabled.Should().BeTrue();
        result.AutoTranscribeOnReady.Should().BeTrue();
        result.DefaultLanguage.Should().Be("en");
        result.OutputFormats.Should().BeEquivalentTo(["vtt", "srt"]);

        updatedSettings.Should().ContainSingle(setting =>
            setting.Key == "transcription.enabled" &&
            setting.Value == "True");
        addedSettings.Should().Contain(setting => setting.Key == "transcription.provider" && setting.Value == "local-faster-whisper");
        addedSettings.Should().Contain(setting => setting.Key == "transcription.localFasterWhisper.model" && setting.Value == "medium");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchVideoTranscript_ShouldAuthorizeAndReturnPagedChunkResults()
    {
        var videoId = Guid.NewGuid();
        var transcriptionId = Guid.NewGuid();
        var chunk = VideoTranscriptChunk.Create(videoId, transcriptionId, "en", 12, 18, "hello world");

        var chunksRepo = Substitute.For<IVideoTranscriptChunkRepository>();
        chunksRepo.SearchKeywordAsync(videoId, "hello", "en", 1, 100, Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<VideoTranscriptChunk>([chunk], 1, 1, 100));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoTranscriptChunks.Returns(chunksRepo);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Viewer);
        currentUser.IsAuthenticated.Returns(true);

        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanViewVideoAsync(videoId, currentUser.UserId, currentUser.Role, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var service = new SearchVideoTranscriptService(unitOfWork, currentUser, authorization);

        var result = await service.Handle(videoId, "hello", "en", 0, 999, null, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Content.Should().Be("hello world");
        result.Items[0].StartSeconds.Should().Be(12);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
        await chunksRepo.Received(1).SearchKeywordAsync(videoId, "hello", "en", 1, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVideoTranscriptionChunks_ShouldFallBackToVideoLanguageChunksForSiblingArtifact()
    {
        var videoId = Guid.NewGuid();
        var primaryTranscriptionId = Guid.NewGuid();
        var siblingTranscriptionId = Guid.NewGuid();

        var siblingTranscription = VideoTranscription.Create(
            videoId,
            "en",
            "srt",
            @"videos\video\transcriptions\en\captions.srt",
            "local-faster-whisper");

        var chunk = VideoTranscriptChunk.Create(videoId, primaryTranscriptionId, "en", 5, 10, "chunk one");

        var transcriptionsRepo = Substitute.For<IVideoTranscriptionRepository>();
        transcriptionsRepo.GetByIdAsync(siblingTranscriptionId, Arg.Any<CancellationToken>())
            .Returns(siblingTranscription);

        var chunksRepo = Substitute.For<IVideoTranscriptChunkRepository>();
        chunksRepo.GetByTranscriptionIdAsync(siblingTranscriptionId, Arg.Any<CancellationToken>())
            .Returns([]);
        chunksRepo.GetByVideoAndLanguageAsync(videoId, "en", Arg.Any<CancellationToken>())
            .Returns([chunk]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoTranscriptions.Returns(transcriptionsRepo);
        unitOfWork.VideoTranscriptChunks.Returns(chunksRepo);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Role.Returns(UserRole.Viewer);
        currentUser.IsAuthenticated.Returns(true);

        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanViewVideoAsync(videoId, currentUser.UserId, currentUser.Role, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var service = new GetVideoTranscriptionChunksService(unitOfWork, currentUser, authorization);

        var result = await service.Handle(videoId, siblingTranscriptionId, null, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Content.Should().Be("chunk one");
        result[0].TranscriptionId.Should().Be(primaryTranscriptionId);
        await chunksRepo.Received(1).GetByTranscriptionIdAsync(siblingTranscriptionId, Arg.Any<CancellationToken>());
        await chunksRepo.Received(1).GetByVideoAndLanguageAsync(videoId, "en", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteVideoTranscriptionCallback_ShouldImportArtifactsAndPersistTranscriptChunks()
    {
        var videoId = Guid.NewGuid();
        var correlationId = "corr-1";
        var workerJobId = "job-1";

        var vttRow = VideoTranscription.Create(videoId, "auto", "vtt", @"videos\video\captions.vtt", "local-faster-whisper");
        vttRow.QueueForProcessing(vttRow.StoragePath, "local-faster-whisper", correlationId, "medium");
        vttRow.StartProcessing(workerJobId);

        var srtRow = VideoTranscription.Create(videoId, "auto", "srt", @"videos\video\captions.srt", "local-faster-whisper");
        srtRow.QueueForProcessing(srtRow.StoragePath, "local-faster-whisper", correlationId, "medium");
        srtRow.StartProcessing(workerJobId);

        var transcriptionsRepo = Substitute.For<IVideoTranscriptionRepository>();
        transcriptionsRepo.GetByWorkerJobIdAsync(workerJobId, Arg.Any<CancellationToken>())
            .Returns([vttRow, srtRow]);

        var chunksRepo = Substitute.For<IVideoTranscriptChunkRepository>();
        var persistedChunks = new List<VideoTranscriptChunk>();
        chunksRepo.AddAsync(Arg.Any<VideoTranscriptChunk>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var chunk = call.Arg<VideoTranscriptChunk>();
                persistedChunks.Add(chunk);
                return Task.FromResult(chunk);
            });

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoTranscriptions.Returns(transcriptionsRepo);
        unitOfWork.VideoTranscriptChunks.Returns(chunksRepo);

        var storageService = Substitute.For<IStorageService>();
        storageService.ImportFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<string>(1));

        var tempDir = Path.Combine(Path.GetTempPath(), $"streamforge-transcription-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var srtPath = Path.Combine(tempDir, "captions.srt");
            var vttPath = Path.Combine(tempDir, "captions.vtt");
            var segmentsPath = Path.Combine(tempDir, "segments.json");
            await File.WriteAllTextAsync(srtPath, "1");
            await File.WriteAllTextAsync(vttPath, "WEBVTT");
            await File.WriteAllTextAsync(
                segmentsPath,
                """
                [
                  { "startSeconds": 0.0, "endSeconds": 4.5, "text": "Hello there" },
                  { "startSeconds": 4.5, "endSeconds": 8.0, "text": "General Kenobi" }
                ]
                """);

            var service = new CompleteVideoTranscriptionCallbackService(
                unitOfWork,
                storageService,
                Substitute.For<ILogger<CompleteVideoTranscriptionCallbackService>>());

            await service.Handle(
                new TranscriptionCallbackRequestDto(
                    correlationId,
                    videoId,
                    workerJobId,
                    "completed",
                    "en",
                    [
                        new TranscriptionCallbackArtifactDto("local_path", srtPath),
                        new TranscriptionCallbackArtifactDto("local_path", vttPath),
                        new TranscriptionCallbackArtifactDto("local_path", segmentsPath)
                    ],
                    null,
                    "local-faster-whisper",
                    "medium"),
                CancellationToken.None);

            vttRow.Status.Should().Be(TranscriptionStatus.Completed);
            srtRow.Status.Should().Be(TranscriptionStatus.Completed);
            vttRow.Language.Should().Be("en");
            srtRow.Language.Should().Be("en");

            await chunksRepo.Received(1).DeleteByVideoAndLanguageAsync(videoId, "en", Arg.Any<CancellationToken>());
            persistedChunks.Should().HaveCount(2);
            persistedChunks.Should().OnlyContain(chunk => chunk.VideoId == videoId && chunk.TranscriptionId == vttRow.Id && chunk.Language == "en");
            persistedChunks.Select(chunk => chunk.Content).Should().BeEquivalentTo(["Hello there", "General Kenobi"]);
            await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ListAdminTranscriptionJobs_ShouldReturnPagedJobsWithVideoTitle()
    {
        var video = Video.Create("Admin Video", null, Guid.NewGuid(), status: VideoStatus.Ready);
        var vttRow = VideoTranscription.Create(video.Id, "en", "vtt", @"videos\video\captions.vtt", "local-faster-whisper");
        var srtRow = VideoTranscription.Create(video.Id, "en", "srt", @"videos\video\captions.srt", "local-faster-whisper");

        vttRow.QueueForProcessing(vttRow.StoragePath, "local-faster-whisper", "corr-1", "small");
        vttRow.StartProcessing("worker-1");
        srtRow.QueueForProcessing(srtRow.StoragePath, "local-faster-whisper", "corr-1", "small");
        srtRow.StartProcessing("worker-1");

        SetPrivateProperty(vttRow, nameof(VideoTranscription.Video), video);
        SetPrivateProperty(srtRow, nameof(VideoTranscription.Video), video);

        var transcriptionsRepo = Substitute.For<IVideoTranscriptionRepository>();
        transcriptionsRepo.QueryAdminRowsAsync(Arg.Any<AdminTranscriptionJobsQuery>(), Arg.Any<CancellationToken>())
            .Returns([vttRow, srtRow]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoTranscriptions.Returns(transcriptionsRepo);

        var provider = Substitute.For<ITranscriptionProvider>();
        provider.GetJobStatusAsync("worker-1", Arg.Any<CancellationToken>())
            .Returns(new TranscriptionProviderJobStatus(
                "worker-1",
                "corr-1",
                "running",
                55,
                "transcribing",
                null,
                "en",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                null,
                120,
                66));

        var service = new ListAdminTranscriptionJobsService(
            unitOfWork,
            provider,
            CreateReconciler(unitOfWork, provider));

        var result = await service.Handle(new AdminTranscriptionJobsQueryDto(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].VideoTitle.Should().Be("Admin Video");
        result.Items[0].WorkerJobId.Should().Be("worker-1");
        result.Items[0].Artifacts.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAdminTranscriptionJobs_ShouldRejectUnsupportedSortBy()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = new ListAdminTranscriptionJobsService(
            unitOfWork,
            Substitute.For<ITranscriptionProvider>(),
            CreateReconciler(unitOfWork, Substitute.For<ITranscriptionProvider>()));

        var act = () => service.Handle(
            new AdminTranscriptionJobsQueryDto
            {
                SortBy = "provider"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static ReconcileTranscriptionOrphansService CreateReconciler(
        IUnitOfWork unitOfWork,
        ITranscriptionProvider transcriptionProvider)
    {
        return new ReconcileTranscriptionOrphansService(
            unitOfWork,
            transcriptionProvider,
            new CompleteVideoTranscriptionCallbackService(
                unitOfWork,
                Substitute.For<IStorageService>(),
                Substitute.For<ILogger<CompleteVideoTranscriptionCallbackService>>()),
            Substitute.For<ILogger<ReconcileTranscriptionOrphansService>>());
    }

    private static void ForceTranscriptionState(
        VideoTranscription transcription,
        TranscriptionStatus status,
        string? workerJobId,
        string? correlationId)
    {
        SetPrivateProperty(transcription, nameof(VideoTranscription.Status), status);
        SetPrivateProperty(transcription, nameof(VideoTranscription.WorkerJobId), workerJobId);
        SetPrivateProperty(transcription, nameof(VideoTranscription.CorrelationId), correlationId);
        SetPrivateProperty(transcription, nameof(VideoTranscription.UpdatedAt), DateTime.UtcNow.AddMinutes(-10));
    }

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        var property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} not found on {typeof(TTarget).Name}.");
        property.SetValue(target, value);
    }
}
