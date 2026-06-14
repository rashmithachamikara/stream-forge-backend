using FluentAssertions;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Infrastructure.Persistence;
using StreamForge.Infrastructure.Tests.Support;

namespace StreamForge.Infrastructure.Tests.Persistence;

[Collection(PostgresIntegrationCollection.Name)]
[Trait("Category", "PostgresIntegration")]
public sealed class AnalyticsQueryServiceIntegrationTests
{
    private readonly PostgresIntegrationFixture _fixture;

    public AnalyticsQueryServiceIntegrationTests(PostgresIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task AnalyticsQueryService_ShouldAggregateQualifiedViewsAndEngagement()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var viewer = User.Create("Viewer", "viewer@example.com", "hash", UserRole.Viewer);
        var category = Category.Create("Education");
        var tag = Tag.Create("backend");
        var video = Video.Create("Analytics Video", null, owner.Id, category.Id, VideoVisibility.Public, VideoStatus.Ready);
        var now = DateTime.UtcNow;
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();
        var ignoredShortSession = Guid.NewGuid();
        context.AddRange(owner, viewer, category, tag, video);
        context.VideoTags.Add(VideoTag.Create(video.Id, tag.Id));
        context.AnalyticsEvents.AddRange(
            AnalyticsEvent.Create(video.Id, session1, AnalyticsEventType.Play, "127.0.0.1", viewer.Id, now.AddMinutes(-20), durationWatched: 20, userAgent: ChromeDesktop()),
            AnalyticsEvent.Create(video.Id, session1, AnalyticsEventType.Complete, "127.0.0.1", viewer.Id, now.AddMinutes(-19), durationWatched: 15, userAgent: ChromeDesktop()),
            AnalyticsEvent.Create(video.Id, session2, AnalyticsEventType.Play, "127.0.0.2", null, now.AddMinutes(-10), durationWatched: 40, userAgent: SafariMobile()),
            AnalyticsEvent.Create(video.Id, ignoredShortSession, AnalyticsEventType.Play, "127.0.0.3", null, now.AddMinutes(-5), durationWatched: 5, userAgent: ChromeDesktop()));
        context.VideoReactions.Add(VideoReaction.Create(viewer.Id, video.Id, ReactionType.Like));
        context.VideoComments.Add(VideoComment.Create(viewer.Id, video.Id, "Good"));
        await context.SaveChangesAsync();
        var service = new AnalyticsQueryService(context);

        var summary = await service.GetVideoSummaryAsync(video.Id, now.AddHours(-1), now.AddHours(1), minimumViewWatchSeconds: 30);
        var engagement = await service.GetVideoEngagementSummaryAsync(video.Id, now.AddHours(-1), now.AddHours(1), minimumViewWatchSeconds: 30);
        var authBreakdown = await service.GetAuthBreakdownAsync(owner.Id, now.AddHours(-1), now.AddHours(1), minimumViewWatchSeconds: 30);
        var categoryBreakdown = await service.GetCategoryBreakdownAsync(owner.Id, now.AddHours(-1), now.AddHours(1), minimumViewWatchSeconds: 30);
        var tagBreakdown = await service.GetTagBreakdownAsync(owner.Id, now.AddHours(-1), now.AddHours(1), minimumViewWatchSeconds: 30);
        var browserBreakdown = await service.GetBrowserBreakdownAsync(owner.Id, now.AddHours(-1), now.AddHours(1), minimumViewWatchSeconds: 30);
        var deviceBreakdown = await service.GetDeviceBreakdownAsync(owner.Id, now.AddHours(-1), now.AddHours(1));

        summary.TotalViews.Should().Be(2);
        summary.UniqueViewers.Should().Be(2);
        summary.TotalWatchTime.Should().Be(75);
        summary.CompletionCount.Should().Be(1);
        engagement.LikeCount.Should().Be(1);
        engagement.CommentCount.Should().Be(1);
        engagement.EngagementScore.Should().Be(2);
        authBreakdown.AuthenticatedViewCount.Should().Be(1);
        authBreakdown.AnonymousViewCount.Should().Be(1);
        categoryBreakdown.Should().ContainSingle(item => item.CategoryId == category.Id && item.ViewCount == 2);
        tagBreakdown.Should().ContainSingle(item => item.TagId == tag.Id && item.ViewCount == 2);
        browserBreakdown.Should().Contain(item => item.BrowserFamily == "chrome" && item.ViewerCount == 1);
        browserBreakdown.Should().Contain(item => item.BrowserFamily == "safari" && item.ViewerCount == 1);
        deviceBreakdown.Should().Contain(item => item.DeviceType == "desktop");
        deviceBreakdown.Should().Contain(item => item.DeviceType == "mobile");
    }

    [IntegrationFact]
    public async Task AnalyticsQueryService_ShouldRankVideosByEngagement()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var viewer = User.Create("Viewer", "viewer@example.com", "hash", UserRole.Viewer);
        var quiet = Video.Create("Quiet", null, owner.Id, status: VideoStatus.Ready);
        var busy = Video.Create("Busy", null, owner.Id, status: VideoStatus.Ready);
        var now = DateTime.UtcNow;
        context.AddRange(owner, viewer, quiet, busy);
        context.AnalyticsEvents.AddRange(
            AnalyticsEvent.Create(quiet.Id, Guid.NewGuid(), AnalyticsEventType.Play, "127.0.0.1", viewer.Id, now, durationWatched: 30, userAgent: ChromeDesktop()),
            AnalyticsEvent.Create(busy.Id, Guid.NewGuid(), AnalyticsEventType.Play, "127.0.0.2", viewer.Id, now, durationWatched: 30, userAgent: ChromeDesktop()));
        context.VideoReactions.Add(VideoReaction.Create(viewer.Id, busy.Id, ReactionType.Like));
        context.VideoComments.Add(VideoComment.Create(viewer.Id, busy.Id, "Nice"));
        await context.SaveChangesAsync();
        var service = new AnalyticsQueryService(context);

        var result = await service.GetRankedVideosAsync(owner.Id, AnalyticsRankingType.MostEngaged, now.AddHours(-1), now.AddHours(1), 30, page: 1, pageSize: 10);

        result.TotalCount.Should().Be(2);
        result.Items.Select(item => item.Title).Should().Equal("Busy", "Quiet");
    }

    private static string ChromeDesktop() =>
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125.0.0.0 Safari/537.36";

    private static string SafariMobile() =>
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Version/17.0 Mobile/15E148 Safari/604.1";
}
