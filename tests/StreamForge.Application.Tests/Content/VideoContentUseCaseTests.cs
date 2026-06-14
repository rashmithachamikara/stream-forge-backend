using FluentAssertions;
using NSubstitute;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Content;
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

    private static IUnitOfWork CreateUnitOfWork(
        IVideoRepository videos,
        ICategoryRepository categories,
        ITagRepository tags,
        IVideoTagRepository videoTags)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Videos.Returns(videos);
        unitOfWork.Categories.Returns(categories);
        unitOfWork.Tags.Returns(tags);
        unitOfWork.VideoTags.Returns(videoTags);
        return unitOfWork;
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
