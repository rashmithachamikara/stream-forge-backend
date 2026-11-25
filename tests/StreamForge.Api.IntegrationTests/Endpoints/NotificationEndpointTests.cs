using System.Net;
using FluentAssertions;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Engagement;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class NotificationEndpointTests(ApiIntegrationFixture fixture)
{
    private const string _notificationsUrl = "/api/v1/me/notifications";

    [ApiIntegrationFact]
    public async Task Recipient_ShouldListCountUpdateAndDeleteNotifications()
    {
        await fixture.ResetDatabaseAsync();
        var recipient = await fixture.CreateUserAsync(UserRole.Viewer, "notification-recipient@example.com");
        var token = recipient.AccessToken;
        var client = fixture.Client;
        var first = Notification.Create(recipient.User.Id, NotificationType.Comment, "First unread notification");
        var second = Notification.Create(recipient.User.Id, NotificationType.Like, "Second unread notification");
        var alreadyRead = Notification.Create(recipient.User.Id, NotificationType.Reply, "Read notification");
        alreadyRead.MarkAsRead();
        await SeedNotificationsAsync(first, second, alreadyRead);

        var initial = await client.ReadOkJsonAsync<PagedResponseDto<NotificationDto>>(HttpMethod.Get, _notificationsUrl, token);
        initial.TotalCount.Should().Be(3);
        initial.Items.Select(item => item.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id, alreadyRead.Id });
        await AssertUnreadCountAsync(token, 2);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, $"{_notificationsUrl}/{first.Id}/read", token);
        await AssertUnreadCountAsync(token, 1);
        var read = await client.ReadOkJsonAsync<PagedResponseDto<NotificationDto>>(HttpMethod.Get, $"{_notificationsUrl}?isRead=true", token);
        read.Items.Select(item => item.Id).Should().BeEquivalentTo(new[] { first.Id, alreadyRead.Id });
        read.Items.Should().OnlyContain(item => item.IsRead);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, $"{_notificationsUrl}/{first.Id}/unread", token);
        await AssertUnreadCountAsync(token, 2);
        var unread = await client.ReadOkJsonAsync<PagedResponseDto<NotificationDto>>(HttpMethod.Get, $"{_notificationsUrl}?isRead=false", token);
        unread.Items.Select(item => item.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
        unread.Items.Should().OnlyContain(item => !item.IsRead);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, $"{_notificationsUrl}/mark-all-read", token);
        await AssertUnreadCountAsync(token, 0);
        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, $"{_notificationsUrl}/{second.Id}/unread", token);
        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Delete, $"{_notificationsUrl}/read", token);
        var remaining = await client.ReadOkJsonAsync<PagedResponseDto<NotificationDto>>(HttpMethod.Get, _notificationsUrl, token);
        remaining.Items.Should().ContainSingle(item => item.Id == second.Id && !item.IsRead);
        remaining.TotalCount.Should().Be(1);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Delete, $"{_notificationsUrl}/{second.Id}", token);
        var empty = await client.ReadOkJsonAsync<PagedResponseDto<NotificationDto>>(HttpMethod.Get, _notificationsUrl, token);
        empty.Items.Should().BeEmpty();
        await AssertUnreadCountAsync(token, 0);
    }

    [ApiIntegrationFact]
    public async Task Notifications_ShouldRestrictReadsAndChangesToTheRecipient()
    {
        await fixture.ResetDatabaseAsync();
        var recipient = await fixture.CreateUserAsync(UserRole.Viewer, "notification-recipient@example.com");
        var otherUser = await fixture.CreateUserAsync(UserRole.Viewer, "notification-other@example.com");
        var client = fixture.Client;
        var own = Notification.Create(recipient.User.Id, NotificationType.Comment, "Own notification");
        var otherUnread = Notification.Create(otherUser.User.Id, NotificationType.Comment, "Other user's unread notification");
        var otherRead = Notification.Create(otherUser.User.Id, NotificationType.Like, "Other user's read notification");
        otherRead.MarkAsRead();
        await SeedNotificationsAsync(own, otherUnread, otherRead);

        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, _notificationsUrl);
        var listed = await client.ReadOkJsonAsync<PagedResponseDto<NotificationDto>>(HttpMethod.Get, _notificationsUrl, recipient.AccessToken);
        listed.Items.Should().ContainSingle(item => item.Id == own.Id);
        listed.TotalCount.Should().Be(1);
        await AssertUnreadCountAsync(recipient.AccessToken, 1);

        await client.AssertStatusAsync(HttpStatusCode.NotFound, HttpMethod.Post, $"{_notificationsUrl}/{otherUnread.Id}/read", recipient.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.NotFound, HttpMethod.Post, $"{_notificationsUrl}/{otherRead.Id}/unread", recipient.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.NotFound, HttpMethod.Delete, $"{_notificationsUrl}/{otherRead.Id}", recipient.AccessToken);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, $"{_notificationsUrl}/mark-all-read", recipient.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Delete, $"{_notificationsUrl}/read", recipient.AccessToken);
        var otherList = await client.ReadOkJsonAsync<PagedResponseDto<NotificationDto>>(HttpMethod.Get, _notificationsUrl, otherUser.AccessToken);
        otherList.TotalCount.Should().Be(2);
        otherList.Items.Should().Contain(item => item.Id == otherUnread.Id && !item.IsRead);
        otherList.Items.Should().Contain(item => item.Id == otherRead.Id && item.IsRead);
        await AssertUnreadCountAsync(otherUser.AccessToken, 1);
    }

    private async Task AssertUnreadCountAsync(string accessToken, int expected)
    {
        var count = await fixture.Client.ReadOkJsonAsync<UnreadNotificationCountDto>(HttpMethod.Get, $"{_notificationsUrl}/unread-count", accessToken);
        count.UnreadCount.Should().Be(expected);
    }

    private async Task SeedNotificationsAsync(params Notification[] notifications)
    {
        await using var context = fixture.CreateContext();
        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();
    }
}
