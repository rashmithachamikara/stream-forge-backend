using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Processing;
using StreamForge.Application.UseCases.Transcriptions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Processing;

public sealed class VideoProcessingUseCaseTests
{
    [Fact]
    public async Task ListAdminVideoProcessingJobs_ShouldReturnOrderedJobs()
    {
        var olderVideo = Video.Create("Older Video", null, Guid.NewGuid(), status: VideoStatus.Failed);
        var newerVideo = Video.Create("Newer Video", null, Guid.NewGuid(), status: VideoStatus.Processing);
        var olderJob = VideoProcessingJob.Create(olderVideo.Id, ProcessingJobType.Transcode);
        var newerJob = VideoProcessingJob.Create(newerVideo.Id, ProcessingJobType.Transcode);

        AttachJobToVideo(olderJob, olderVideo);
        AttachJobToVideo(newerJob, newerVideo);

        var jobs = Substitute.For<IVideoProcessingJobRepository>();
        jobs.GetAllOrderedAsync(Arg.Any<CancellationToken>()).Returns([newerJob, olderJob]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoProcessingJobs.Returns(jobs);

        var service = new ListAdminVideoProcessingJobsService(unitOfWork);

        var result = await service.Handle(null, CancellationToken.None);

        result.Select(job => job.JobKey).Should().Equal(newerJob.Id.ToString(), olderJob.Id.ToString());
        result[0].VideoTitle.Should().Be("Newer Video");
        result[0].VideoStatus.Should().Be(VideoStatus.Processing.ToString());
    }

    [Fact]
    public async Task RetryAdminVideoProcessingJob_ShouldResetFailedJobAndEnqueueAgain()
    {
        var video = Video.Create("Retry Video", null, Guid.NewGuid(), status: VideoStatus.Failed);
        var job = VideoProcessingJob.Create(video.Id, ProcessingJobType.Transcode);
        job.Fail("ffmpeg failed");
        AttachJobToVideo(job, video);

        var jobs = Substitute.For<IVideoProcessingJobRepository>();
        jobs.GetWithVideoAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoProcessingJobs.Returns(jobs);

        var queue = Substitute.For<IVideoProcessingQueue>();
        var service = new RetryAdminVideoProcessingJobService(unitOfWork, queue);

        var result = await service.Handle(job.Id.ToString(), CancellationToken.None);

        job.Status.Should().Be(ProcessingJobStatus.Pending);
        job.Progress.Should().Be(0);
        job.ErrorMessage.Should().BeNull();
        video.Status.Should().Be(VideoStatus.Processing);
        result.Status.Should().Be(ProcessingJobStatus.Pending.ToString());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await queue.Received(1).EnqueueAsync(job.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResyncAdminVideoProcessingJob_ShouldMarkVideoReadyWhenCompletedArtifactsExist()
    {
        var video = Video.Create("Ready Video", null, Guid.NewGuid(), status: VideoStatus.Processing);
        var job = VideoProcessingJob.Create(video.Id, ProcessingJobType.Transcode);
        job.Start();
        job.Complete();
        AttachJobToVideo(job, video);

        var jobs = Substitute.For<IVideoProcessingJobRepository>();
        jobs.GetWithVideoAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        var versions = Substitute.For<IVideoVersionRepository>();
        versions.GetByVideoIdAsync(video.Id, Arg.Any<CancellationToken>())
            .Returns([VideoVersion.Create(video.Id, "adaptive", VideoFormat.HLS, "master.m3u8", 100, 120)]);

        var thumbnails = Substitute.For<IVideoThumbnailRepository>();
        thumbnails.GetDefaultByVideoIdAsync(video.Id, Arg.Any<CancellationToken>())
            .Returns(VideoThumbnail.Create(video.Id, "thumb.jpg", 1280, 720, 10, true, 5));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoProcessingJobs.Returns(jobs);
        unitOfWork.VideoVersions.Returns(versions);
        unitOfWork.VideoThumbnails.Returns(thumbnails);

        var service = new ResyncAdminVideoProcessingJobService(unitOfWork);

        var result = await service.Handle(job.Id.ToString(), CancellationToken.None);

        video.Status.Should().Be(VideoStatus.Ready);
        result.VideoStatus.Should().Be(VideoStatus.Ready.ToString());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessVideoJob_ShouldGenerateHlsThumbnailAndMarkVideoReady()
    {
        var video = Video.Create("Video", null, Guid.NewGuid(), status: VideoStatus.Uploading);
        var job = VideoProcessingJob.Create(video.Id, ProcessingJobType.Transcode);
        var originalVersion = VideoVersion.Create(video.Id, "original", VideoFormat.Mp4, "source.mp4", 100, 0);
        var originalFile = VideoFile.Create(originalVersion.Id, Guid.NewGuid(), "source.mp4", 100, "video/mp4");
        var provider = StorageProvider.Create("local", StorageProviderType.Local, "{}", isDefault: true);
        var media = Substitute.For<IMediaProcessingService>();
        media.ProbeAsync("source.mp4", Arg.Any<CancellationToken>())
            .Returns(new MediaProbeResult(120, 1920, 1080, 4500, "h264"));
        media.GenerateHlsAsync(video.Id, "source.mp4", Arg.Any<MediaProbeResult>(), Arg.Any<CancellationToken>())
            .Returns(new HlsOutputResult(
                "processing/master.m3u8",
                100,
                [new HlsVariantResult("720p", 1280, 720, "processing/720p.m3u8", 80, 2500, "h264")]));
        media.GenerateThumbnailAsync(video.Id, "source.mp4", Arg.Any<MediaProbeResult>(), Arg.Any<CancellationToken>())
            .Returns(new ThumbnailOutputResult("thumb.jpg", 1280, 720, 25, 5));
        var videoVersions = Substitute.For<IVideoVersionRepository>();
        videoVersions.GetByIdAsync(originalVersion.Id, Arg.Any<CancellationToken>()).Returns(originalVersion);
        var videoFiles = Substitute.For<IVideoFileRepository>();
        videoFiles.GetOriginalByVideoIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(originalFile);
        var thumbnails = Substitute.For<IVideoThumbnailRepository>();
        var unitOfWork = CreateUnitOfWork(video, job, provider, videoVersions, videoFiles, thumbnails);
        var service = new ProcessVideoJobService(
            unitOfWork,
            media,
            Substitute.For<ITranscriptionQueue>(),
            new ResolveTranscriptionSettingsService(unitOfWork, new TranscriptionOptions()),
            Substitute.For<ILogger<ProcessVideoJobService>>());

        await service.Handle(job.Id, CancellationToken.None);

        video.Status.Should().Be(VideoStatus.Ready);
        job.Status.Should().Be(ProcessingJobStatus.Completed);
        job.Progress.Should().Be(100);
        originalVersion.DurationSeconds.Should().Be(120);
        await videoVersions.Received(2).AddAsync(Arg.Is<VideoVersion>(version =>
            version.VideoId == video.Id &&
            version.Format == VideoFormat.HLS), Arg.Any<CancellationToken>());
        await videoFiles.Received(2).AddAsync(Arg.Any<VideoFile>(), Arg.Any<CancellationToken>());
        await thumbnails.Received(1).AddAsync(Arg.Is<VideoThumbnail>(thumbnail =>
            thumbnail.VideoId == video.Id &&
            thumbnail.StoragePath == "thumb.jpg" &&
            thumbnail.IsDefault), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessVideoJob_ShouldMarkJobAndVideoFailedWhenProcessingThrows()
    {
        var video = Video.Create("Video", null, Guid.NewGuid(), status: VideoStatus.Uploading);
        var job = VideoProcessingJob.Create(video.Id, ProcessingJobType.Transcode);
        var originalVersion = VideoVersion.Create(video.Id, "original", VideoFormat.Mp4, "source.mp4", 100, 0);
        var originalFile = VideoFile.Create(originalVersion.Id, Guid.NewGuid(), "source.mp4", 100, "video/mp4");
        var provider = StorageProvider.Create("local", StorageProviderType.Local, "{}", isDefault: true);
        var media = Substitute.For<IMediaProcessingService>();
        media.ProbeAsync("source.mp4", Arg.Any<CancellationToken>())
            .Returns<Task<MediaProbeResult>>(_ => throw new InvalidOperationException("ffmpeg failed"));
        var videoVersions = Substitute.For<IVideoVersionRepository>();
        videoVersions.GetByIdAsync(originalVersion.Id, Arg.Any<CancellationToken>()).Returns(originalVersion);
        var videoFiles = Substitute.For<IVideoFileRepository>();
        videoFiles.GetOriginalByVideoIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(originalFile);
        var unitOfWork = CreateUnitOfWork(video, job, provider, videoVersions, videoFiles, Substitute.For<IVideoThumbnailRepository>());
        var service = new ProcessVideoJobService(
            unitOfWork,
            media,
            Substitute.For<ITranscriptionQueue>(),
            new ResolveTranscriptionSettingsService(unitOfWork, new TranscriptionOptions()),
            Substitute.For<ILogger<ProcessVideoJobService>>());

        await service.Handle(job.Id, CancellationToken.None);

        video.Status.Should().Be(VideoStatus.Failed);
        job.Status.Should().Be(ProcessingJobStatus.Failed);
        job.ErrorMessage.Should().Be("ffmpeg failed");
    }

    private static IUnitOfWork CreateUnitOfWork(
        Video video,
        VideoProcessingJob job,
        StorageProvider provider,
        IVideoVersionRepository videoVersions,
        IVideoFileRepository videoFiles,
        IVideoThumbnailRepository thumbnails)
    {
        var jobs = Substitute.For<IVideoProcessingJobRepository>();
        jobs.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var videos = Substitute.For<IVideoRepository>();
        videos.GetByIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        var storageProviders = Substitute.For<IStorageProviderRepository>();
        storageProviders.GetDefaultByTypeAsync(StorageProviderType.Local, Arg.Any<CancellationToken>()).Returns(provider);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.VideoProcessingJobs.Returns(jobs);
        unitOfWork.Videos.Returns(videos);
        unitOfWork.VideoVersions.Returns(videoVersions);
        unitOfWork.VideoFiles.Returns(videoFiles);
        unitOfWork.StorageProviders.Returns(storageProviders);
        unitOfWork.VideoThumbnails.Returns(thumbnails);
        unitOfWork.SystemSettings.Returns(Substitute.For<ISystemSettingRepository>());
        return unitOfWork;
    }

    private static void AttachJobToVideo(VideoProcessingJob job, Video video)
    {
        typeof(VideoProcessingJob).GetProperty(nameof(VideoProcessingJob.Video))!
            .SetValue(job, video);
    }
}
