using FluentAssertions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Tests;

public sealed class NotificationTests
{
    [Fact]
    public void Create_ShouldStartUnread()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        var notification = Notification.Create(userId, NotificationType.Comment, "New comment", videoId);

        notification.UserId.Should().Be(userId);
        notification.VideoId.Should().Be(videoId);
        notification.NotificationType.Should().Be(NotificationType.Comment);
        notification.Message.Should().Be("New comment");
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void MarkReadState_ShouldToggleReadState()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.Like, "Liked");

        notification.MarkAsRead();
        notification.MarkAsUnread();

        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldRejectBlankMessage()
    {
        var act = () => Notification.Create(Guid.NewGuid(), NotificationType.Like, " ");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("message");
    }
}
