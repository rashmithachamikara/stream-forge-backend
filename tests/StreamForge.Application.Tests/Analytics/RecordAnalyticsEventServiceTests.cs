using FluentAssertions;
using NSubstitute;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Analytics;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Analytics;

public sealed class RecordAnalyticsEventServiceTests
{
    [Fact]
    public async Task Handle_ShouldRecordPlayEventAndIncrementViewWhenWatchThresholdIsCrossed()
    {
        var userId = Guid.NewGuid();
        var video = Video.Create("Video", null, userId, status: VideoStatus.Ready);
        var analyticsEvents = Substitute.For<IAnalyticsEventRepository>();
        var analyticsQuery = Substitute.For<IAnalyticsQueryService>();
        analyticsQuery.GetSessionWatchTimeAsync(video.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(29);
        var sut = CreateService(
            new AnalyticsOptions { MinimumViewWatchSeconds = 30 },
            currentUserService: CreateCurrentUser(userId),
            authorizationService: CreateAuthorization(canView: true),
            analyticsQueryService: analyticsQuery,
            unitOfWork: CreateUnitOfWork(video, analyticsEvents));
        var request = CreateRequest(AnalyticsEventType.Play, durationWatched: 1);

        var result = await sut.Handle(video.Id, request, CancellationToken.None);

        result.EventRecorded.Should().BeTrue();
        result.ViewCountIncremented.Should().BeTrue();
        video.ViewCount.Should().Be(1);
        await analyticsEvents.Received(1).AddAsync(Arg.Is<AnalyticsEvent>(analyticsEvent =>
            analyticsEvent.VideoId == video.Id &&
            analyticsEvent.SessionId == request.SessionId &&
            analyticsEvent.EventType == AnalyticsEventType.Play &&
            analyticsEvent.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSkipEventStorageWhenRawCollectionIsDisabled()
    {
        var userId = Guid.NewGuid();
        var video = Video.Create("Video", null, userId, status: VideoStatus.Ready);
        var analyticsEvents = Substitute.For<IAnalyticsEventRepository>();
        var sut = CreateService(
            new AnalyticsOptions { CollectRawEvents = false },
            currentUserService: CreateCurrentUser(userId),
            authorizationService: CreateAuthorization(canView: true),
            unitOfWork: CreateUnitOfWork(video, analyticsEvents));

        var result = await sut.Handle(video.Id, CreateRequest(AnalyticsEventType.Play), CancellationToken.None);

        result.EventRecorded.Should().BeFalse();
        result.ViewCountIncremented.Should().BeFalse();
        await analyticsEvents.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRejectWhenAnalyticsAreDisabled()
    {
        var sut = CreateService(new AnalyticsOptions { Enabled = false });
        var request = CreateRequest(AnalyticsEventType.Play);

        var act = () => sut.Handle(Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("Analytics are disabled");
    }

    [Fact]
    public async Task Handle_ShouldRejectDisabledPauseEvents()
    {
        var sut = CreateService(new AnalyticsOptions { CollectPauseEvents = false });
        var request = CreateRequest(AnalyticsEventType.Pause);

        var act = () => sut.Handle(Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("Pause event analytics are disabled");
    }

    [Fact]
    public async Task Handle_ShouldIgnoreAnonymousEventsWhenAnonymousCollectionIsDisabled()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.IsAuthenticated.Returns(false);

        var sut = CreateService(
            new AnalyticsOptions { CollectAnonymousEvents = false },
            currentUserService: currentUserService);
        var request = CreateRequest(AnalyticsEventType.Play);

        var result = await sut.Handle(Guid.NewGuid(), request, CancellationToken.None);

        result.EventRecorded.Should().BeFalse();
        result.ViewCountIncremented.Should().BeFalse();
    }

    [Fact]
    public async Task GetRankedVideos_ShouldNormalizePaginationAndUseConfiguredThreshold()
    {
        var analyticsQuery = Substitute.For<IAnalyticsQueryService>();
        analyticsQuery.GetRankedVideosAsync(
                ownerId: null,
                AnalyticsRankingType.MostWatched,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                minimumViewWatchSeconds: 45,
                page: 1,
                pageSize: 100,
                Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<RankedVideoAnalyticsDto>(
                [new RankedVideoAnalyticsDto(Guid.NewGuid(), "Video", 3, 90, 2, 1, 4, 7)],
                TotalCount: 1,
                Page: 1,
                PageSize: 100));
        var service = new GetRankedVideosAnalyticsService(
            analyticsQuery,
            new AnalyticsOptions { MinimumViewWatchSeconds = 45 });

        var result = await service.Handle(null, AnalyticsRankingType.MostWatched, null, null, page: 0, pageSize: 500, CancellationToken.None);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.EngagementScore.Should().Be(7);
    }

    [Fact]
    public async Task GetDeviceBreakdown_ShouldRejectWhenDisabled()
    {
        var service = new GetDeviceBreakdownAnalyticsService(
            Substitute.For<IAnalyticsQueryService>(),
            new AnalyticsOptions { EnableDeviceBreakdown = false });

        var act = () => service.Handle(null, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("Device breakdown analytics are disabled");
    }

    private static RecordAnalyticsEventService CreateService(
        AnalyticsOptions options,
        ICurrentUserService? currentUserService = null,
        IAuthorizationService? authorizationService = null,
        IAnalyticsQueryService? analyticsQueryService = null,
        IUnitOfWork? unitOfWork = null)
    {
        return new RecordAnalyticsEventService(
            unitOfWork ?? Substitute.For<IUnitOfWork>(),
            currentUserService ?? Substitute.For<ICurrentUserService>(),
            authorizationService ?? Substitute.For<IAuthorizationService>(),
            analyticsQueryService ?? Substitute.For<IAnalyticsQueryService>(),
            CreateRequestMetadata(),
            options);
    }

    private static RecordAnalyticsEventRequestDto CreateRequest(AnalyticsEventType eventType, int durationWatched = 1)
    {
        return new RecordAnalyticsEventRequestDto(
            Guid.NewGuid(),
            eventType,
            DateTime.UtcNow,
            Position: 0,
            DurationWatched: durationWatched);
    }

    private static ICurrentUserService CreateCurrentUser(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.Role.Returns(UserRole.Viewer);
        currentUser.IsAuthenticated.Returns(true);
        return currentUser;
    }

    private static IAuthorizationService CreateAuthorization(bool canView)
    {
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.CanViewVideoAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<UserRole?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(canView);
        return authorization;
    }

    private static IUnitOfWork CreateUnitOfWork(Video video, IAnalyticsEventRepository analyticsEvents)
    {
        var videos = Substitute.For<IVideoRepository>();
        videos.GetByIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Videos.Returns(videos);
        unitOfWork.AnalyticsEvents.Returns(analyticsEvents);
        return unitOfWork;
    }

    private static IRequestMetadataAccessor CreateRequestMetadata()
    {
        var metadata = Substitute.For<IRequestMetadataAccessor>();
        metadata.IpAddress.Returns("127.0.0.1");
        metadata.UserAgent.Returns("UnitTest");
        return metadata;
    }
}
