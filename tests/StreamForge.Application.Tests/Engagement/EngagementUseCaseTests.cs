using FluentAssertions;
using NSubstitute;
using StreamForge.Application.DTOs.Engagement;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Engagement;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Tests.Engagement;

public sealed class EngagementUseCaseTests
{
    [Fact]
    public async Task SetReaction_ShouldCreateLikeNotificationForVideoOwner()
    {
        var viewerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var video = Video.Create("Owner Video", null, ownerId, status: VideoStatus.Ready);
        var reactions = Substitute.For<IVideoReactionRepository>();
        reactions.GetSummaryAsync(video.Id, viewerId, Arg.Any<CancellationToken>())
            .Returns(new ReactionSummaryResult(1, 0, ReactionType.Like));
        var notifications = Substitute.For<INotificationRepository>();
        var service = new SetReactionService(
            CreateUnitOfWork(video, reactions: reactions, notifications: notifications),
            CreateCurrentUser(viewerId, "Rashmi"),
            CreateAuthorization(canView: true));

        var result = await service.Handle(video.Id, new SetReactionRequestDto(ReactionType.Like), CancellationToken.None);

        result.LikeCount.Should().Be(1);
        result.CurrentUserReaction.Should().Be(ReactionType.Like);
        await reactions.Received(1).AddAsync(Arg.Is<VideoReaction>(reaction =>
            reaction.UserId == viewerId &&
            reaction.VideoId == video.Id &&
            reaction.ReactionType == ReactionType.Like), Arg.Any<CancellationToken>());
        await notifications.Received(1).AddAsync(Arg.Is<Notification>(notification =>
            notification.UserId == ownerId &&
            notification.NotificationType == NotificationType.Like &&
            notification.VideoId == video.Id &&
            notification.Message.Contains("Rashmi liked your video")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListComments_ShouldReturnReplyCounts()
    {
        var userId = Guid.NewGuid();
        var video = Video.Create("Video", null, userId, status: VideoStatus.Ready);
        var comment = VideoComment.Create(userId, video.Id, "Nice");
        var comments = Substitute.For<IVideoCommentRepository>();
        comments.GetPagedByVideoIdAsync(video.Id, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedQueryResult<VideoComment>([comment], 1, 1, 20));
        comments.GetReplyCountsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(comment.Id)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [comment.Id] = 2 });
        var service = new ListCommentsService(
            CreateUnitOfWork(video, comments: comments),
            CreateCurrentUser(userId),
            CreateAuthorization(canView: true));

        var result = await service.Handle(new ListCommentsQuery(video.Id, ParentCommentId: null, Page: 1, PageSize: 20), shareToken: null, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle()
            .Which.ReplyCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateBookmark_ShouldPersistTimestampMarkerForCurrentUser()
    {
        var userId = Guid.NewGuid();
        var video = Video.Create("Video", null, Guid.NewGuid(), status: VideoStatus.Ready);
        var bookmarks = Substitute.For<IBookmarkRepository>();
        Bookmark? created = null;
        bookmarks.AddAsync(Arg.Do<Bookmark>(bookmark => created = bookmark), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Bookmark>());
        bookmarks.GetByIdForUserAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(_ => created);
        var service = new CreateBookmarkService(
            CreateUnitOfWork(video, bookmarks: bookmarks),
            CreateCurrentUser(userId),
            CreateAuthorization(canView: true));

        var result = await service.Handle(video.Id, new CreateBookmarkRequestDto(42, "  key moment  "), CancellationToken.None);

        result.VideoId.Should().Be(video.Id);
        result.TimestampSeconds.Should().Be(42);
        result.Note.Should().Be("key moment");
        await bookmarks.Received(1).AddAsync(Arg.Is<Bookmark>(bookmark =>
            bookmark.UserId == userId &&
            bookmark.VideoId == video.Id &&
            bookmark.TimestampSeconds == 42 &&
            bookmark.Note == "key moment"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNotification_ShouldOnlyDeleteCurrentUsersNotification()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create(userId, NotificationType.Comment, "Commented", Guid.NewGuid());
        var notifications = Substitute.For<INotificationRepository>();
        notifications.GetByIdForUserAsync(notification.Id, userId, Arg.Any<CancellationToken>())
            .Returns(notification);
        var service = new DeleteNotificationService(
            CreateUnitOfWork(notifications: notifications),
            CreateCurrentUser(userId));

        await service.Handle(notification.Id, CancellationToken.None);

        await notifications.Received(1).GetByIdForUserAsync(notification.Id, userId, Arg.Any<CancellationToken>());
        await notifications.Received(1).DeleteAsync(notification, Arg.Any<CancellationToken>());
    }

    private static IUnitOfWork CreateUnitOfWork(
        Video? video = null,
        IVideoReactionRepository? reactions = null,
        IVideoCommentRepository? comments = null,
        IBookmarkRepository? bookmarks = null,
        INotificationRepository? notifications = null)
    {
        var videos = Substitute.For<IVideoRepository>();
        if (video is not null)
        {
            videos.GetByIdAsync(video.Id, Arg.Any<CancellationToken>()).Returns(video);
        }

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.Videos.Returns(videos);
        unitOfWork.VideoReactions.Returns(reactions ?? Substitute.For<IVideoReactionRepository>());
        unitOfWork.VideoComments.Returns(comments ?? Substitute.For<IVideoCommentRepository>());
        unitOfWork.Bookmarks.Returns(bookmarks ?? Substitute.For<IBookmarkRepository>());
        unitOfWork.Notifications.Returns(notifications ?? Substitute.For<INotificationRepository>());
        return unitOfWork;
    }

    private static ICurrentUserService CreateCurrentUser(Guid userId, string? name = null)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        currentUser.Role.Returns(UserRole.Viewer);
        currentUser.Name.Returns(name);
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
}
