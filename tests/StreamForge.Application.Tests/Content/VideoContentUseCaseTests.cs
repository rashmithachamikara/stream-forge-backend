using FluentAssertions;
using NSubstitute;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Content;
using StreamForge.Application.UseCases.Processing;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Content;

public sealed class VideoContentUseCaseTests
{
    [Fact]
    public async Task UpdateVideo_ShouldApplyMetadataPlayerSettingsAndTags()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var tag = Tag.Create("backend");
        var video = Video.Create("Old", "Old description", userId, status: VideoStatus.Ready);
        var videos = Substitute.For<IVideoRepository>();
        videos.GetByIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        videos.GetWithDetailsAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        var categories = Substitute.For<ICategoryRepository>();
        categories.ExistsAsync(categoryId, Arg.Any<CancellationToken>()).Returns(true);
        var tags = Substitute.For<ITagRepository>();
        tags.ExistsAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(true);
        tags.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(tag);
        var videoTags = Substitute.For<IVideoTagRepository>();
        videoTags.GetByVideoIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns([]);
        var service = new UpdateVideoService(
            CreateUnitOfWork(videos, categories, tags, videoTags),
            CreateCurrentUser(userId),
            CreateAuthorization(canManage: true));
        var request = new UpdateVideoRequestDto(
            "New",
            "New description",
            categoryId,
            VideoVisibility.Internal,
            [tag.Id],
            AllowComments: false,
            AllowLikes: false,
            Autoplay: true,
            Loop: true,
            DefaultVolume: 55,
            CaptionsEnabled: false,
            PlayerTheme: "cinema");

        var result = await service.Handle(video.Id, request, CancellationToken.None);

        result.Title.Should().Be("New");
        result.Description.Should().Be("New description");
        result.CategoryId.Should().Be(categoryId);
        result.Visibility.Should().Be(VideoVisibility.Internal);
        result.AllowComments.Should().BeFalse();
        result.AllowLikes.Should().BeFalse();
        result.Autoplay.Should().BeTrue();
        result.Loop.Should().BeTrue();
        result.DefaultVolume.Should().Be(55);
        result.CaptionsEnabled.Should().BeFalse();
        result.PlayerTheme.Should().Be("cinema");
        tag.UsageCount.Should().Be(1);
        await videoTags.Received(1).DeleteByVideoIdAsync(video.Id, Arg.Any<CancellationToken>());
        await videoTags.Received(1).AddAsync(Arg.Is<VideoTag>(videoTag =>
            videoTag.VideoId == video.Id &&
            videoTag.TagId == tag.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArchiveVideo_ShouldMarkVideoDeletedWhenUserCanManage()
    {
        var userId = Guid.NewGuid();
        var video = Video.Create("Video", null, userId, status: VideoStatus.Ready);
        var videos = Substitute.For<IVideoRepository>();
        videos.GetByIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        var service = new ArchiveVideoService(
            CreateUnitOfWork(videos, Substitute.For<ICategoryRepository>(), Substitute.For<ITagRepository>(), Substitute.For<IVideoTagRepository>()),
            CreateCurrentUser(userId),
            CreateAuthorization(canManage: true));

        await service.Handle(video.Id, CancellationToken.None);

        video.Status.Should().Be(VideoStatus.Deleted);
    }

    [Fact]
    public async Task GetVideoProcessingStatus_ShouldReconcileCompletedArtifactsToReady()
    {
        var userId = Guid.NewGuid();
        var video = Video.Create("Video", null, userId, status: VideoStatus.Processing);
        var job = VideoProcessingJob.Create(video.Id, ProcessingJobType.Transcode);
        job.Start();
        job.UpdateProgress(20);
        AttachJobToVideo(job, video);

        var videos = Substitute.For<IVideoRepository>();
        videos.GetByIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        var processingJobs = Substitute.For<IVideoProcessingJobRepository>();
        processingJobs.GetLatestByVideoIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(job);
        var versions = Substitute.For<IVideoVersionRepository>();
        versions.GetByVideoIdAsync(video.Id, Arg.Any<CancellationToken>())
            .Returns([VideoVersion.Create(video.Id, "adaptive", VideoFormat.HLS, "master.m3u8", 100, 120)]);
        var thumbnails = Substitute.For<IVideoThumbnailRepository>();
        thumbnails.GetDefaultByVideoIdAsync(video.Id, Arg.Any<CancellationToken>())
            .Returns(VideoThumbnail.Create(video.Id, "thumb.jpg", 1280, 720, 10, true, 5));
        var runtimeMonitor = Substitute.For<IVideoProcessingRuntimeMonitor>();
        runtimeMonitor.HasActiveExecutionAsync(job.Id, Arg.Any<CancellationToken>()).Returns(false);
        var unitOfWork = CreateUnitOfWork(
            videos,
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITagRepository>(),
            Substitute.For<IVideoTagRepository>(),
            processingJobs,
            versions,
            thumbnails);

        var service = new GetVideoProcessingStatusService(
            unitOfWork,
            CreateCurrentUser(userId),
            CreateAuthorization(canManage: true),
            new ReconcileVideoProcessingOrphansService(unitOfWork, runtimeMonitor));

        var result = await service.Handle(video.Id, CancellationToken.None);

        result.VideoStatus.Should().Be(VideoStatus.Ready);
        result.JobStatus.Should().Be(ProcessingJobStatus.Completed.ToString());
        result.Progress.Should().Be(100);
    }

    [Fact]
    public async Task GetVideoProcessingStatus_ShouldReconcileStaleProcessingJobToFailed()
    {
        var userId = Guid.NewGuid();
        var video = Video.Create("Video", null, userId, status: VideoStatus.Processing);
        var job = VideoProcessingJob.Create(video.Id, ProcessingJobType.Transcode);
        job.Start();
        job.UpdateProgress(20);
        AttachJobToVideo(job, video);

        var videos = Substitute.For<IVideoRepository>();
        videos.GetByIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        var processingJobs = Substitute.For<IVideoProcessingJobRepository>();
        processingJobs.GetLatestByVideoIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(job);
        var versions = Substitute.For<IVideoVersionRepository>();
        versions.GetByVideoIdAsync(video.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VideoVersion>());
        var thumbnails = Substitute.For<IVideoThumbnailRepository>();
        thumbnails.GetDefaultByVideoIdAsync(video.Id, Arg.Any<CancellationToken>())
            .Returns((VideoThumbnail?)null);
        var runtimeMonitor = Substitute.For<IVideoProcessingRuntimeMonitor>();
        runtimeMonitor.HasActiveExecutionAsync(job.Id, Arg.Any<CancellationToken>()).Returns(false);
        var unitOfWork = CreateUnitOfWork(
            videos,
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITagRepository>(),
            Substitute.For<IVideoTagRepository>(),
            processingJobs,
            versions,
            thumbnails);

        var service = new GetVideoProcessingStatusService(
            unitOfWork,
            CreateCurrentUser(userId),
            CreateAuthorization(canManage: true),
            new ReconcileVideoProcessingOrphansService(unitOfWork, runtimeMonitor));

        var result = await service.Handle(video.Id, CancellationToken.None);

        result.VideoStatus.Should().Be(VideoStatus.Failed);
        result.JobStatus.Should().Be(ProcessingJobStatus.Failed.ToString());
        result.ErrorMessage.Should().Be("Processing job was interrupted or orphaned before completion.");
    }

    private static IUnitOfWork CreateUnitOfWork(
        IVideoRepository videos,
        ICategoryRepository categories,
        ITagRepository tags,
        IVideoTagRepository videoTags,
        IVideoProcessingJobRepository? processingJobs = null,
        IVideoVersionRepository? videoVersions = null,
        IVideoThumbnailRepository? videoThumbnails = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Videos.Returns(videos);
        unitOfWork.Categories.Returns(categories);
        unitOfWork.Tags.Returns(tags);
        unitOfWork.VideoTags.Returns(videoTags);
        unitOfWork.VideoProcessingJobs.Returns(processingJobs ?? Substitute.For<IVideoProcessingJobRepository>());
        unitOfWork.VideoVersions.Returns(videoVersions ?? Substitute.For<IVideoVersionRepository>());
        unitOfWork.VideoThumbnails.Returns(videoThumbnails ?? Substitute.For<IVideoThumbnailRepository>());
        return unitOfWork;
    }

    private static void AttachJobToVideo(VideoProcessingJob job, Video video)
    {
        typeof(VideoProcessingJob).GetProperty(nameof(VideoProcessingJob.Video))!
            .SetValue(job, video);
    }

    private static ICurrentUserService CreateCurrentUser(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.Role.Returns(UserRole.Editor);
        currentUser.IsAuthenticated.Returns(true);
        return currentUser;
    }

    private static IAuthorizationService CreateAuthorization(bool canManage)
    {
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanManageVideoAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<UserRole?>(),
                Arg.Any<CancellationToken>())
            .Returns(canManage);
        return authorization;
    }
}
