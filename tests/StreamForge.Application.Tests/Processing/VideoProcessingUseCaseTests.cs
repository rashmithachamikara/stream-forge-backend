using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Processing;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Processing;

public sealed class VideoProcessingUseCaseTests
{
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
        var service = new ProcessVideoJobService(
            CreateUnitOfWork(video, job, provider, videoVersions, videoFiles, thumbnails),
            media,
            Substitute.For<ITranscriptionQueue>(),
            new TranscriptionOptions(),
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
        var service = new ProcessVideoJobService(
            CreateUnitOfWork(video, job, provider, videoVersions, videoFiles, Substitute.For<IVideoThumbnailRepository>()),
            media,
            Substitute.For<ITranscriptionQueue>(),
            new TranscriptionOptions(),
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
        return unitOfWork;
    }
}
