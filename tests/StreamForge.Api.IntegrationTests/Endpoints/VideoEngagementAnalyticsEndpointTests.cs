using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Engagement;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class VideoEngagementAnalyticsEndpointTests
{
    private readonly ApiIntegrationFixture _fixture;

    public VideoEngagementAnalyticsEndpointTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [ApiIntegrationFact]
    public async Task VideoEndpoints_ShouldListDetailsAndProtectProcessingStatus()
    {
        await _fixture.ResetDatabaseAsync();
        var owner = await _fixture.CreateUserAsync(UserRole.Editor, "owner-video@example.com");
        var video = await SeedReadyVideoAsync(owner.User.Id, "Public API Video");

        var list = await _fixture.Client.GetAsync("/api/v1/videos?search=api&page=1&pageSize=10");
        var details = await _fixture.Client.GetAsync($"/api/v1/videos/{video.Id}");
        var unauthorizedStatus = await _fixture.Client.GetAsync($"/api/v1/videos/{video.Id}/processing-status");
        var authorizedStatus = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Get,
            $"/api/v1/videos/{video.Id}/processing-status",
            accessToken: owner.AccessToken));

        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await list.Content.ReadFromJsonAsync<PagedResponseDto<VideoSummaryDto>>(ApiTestClient.JsonOptions);
        page!.Items.Should().ContainSingle(item => item.Id == video.Id);
        details.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await details.Content.ReadFromJsonAsync<VideoDetailDto>(ApiTestClient.JsonOptions);
        detail!.Title.Should().Be("Public API Video");
        unauthorizedStatus.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        authorizedStatus.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [ApiIntegrationFact]
    public async Task EngagementEndpoints_ShouldSupportReactionCommentAndTimestampBookmark()
    {
        await _fixture.ResetDatabaseAsync();
        var owner = await _fixture.CreateUserAsync(UserRole.Editor, "owner-engagement@example.com");
        var viewer = await _fixture.CreateUserAsync(UserRole.Viewer, "viewer-engagement@example.com");
        var video = await SeedReadyVideoAsync(owner.User.Id, "Engagement Video");

        var reaction = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Put,
            $"/api/v1/videos/{video.Id}/reaction",
            new SetReactionRequestDto(ReactionType.Like),
            viewer.AccessToken));
        var comment = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Post,
            $"/api/v1/videos/{video.Id}/comments",
            new CreateCommentRequestDto("Great video", ParentCommentId: null),
            viewer.AccessToken));
        var bookmark = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Post,
            $"/api/v1/videos/{video.Id}/bookmarks",
            new CreateBookmarkRequestDto(12, "Important moment"),
            viewer.AccessToken));
        var bookmarks = await _fixture.Client.SendAsync(ApiTestClient.JsonRequest(
            HttpMethod.Get,
            $"/api/v1/videos/{video.Id}/bookmarks",
            accessToken: viewer.AccessToken));

        reaction.StatusCode.Should().Be(HttpStatusCode.OK);
        var reactionSummary = await reaction.Content.ReadFromJsonAsync<ReactionSummaryDto>(ApiTestClient.JsonOptions);
        reactionSummary!.LikeCount.Should().Be(1);
        comment.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdComment = await comment.Content.ReadFromJsonAsync<CommentDto>(ApiTestClient.JsonOptions);
        createdComment!.Comment.Should().Be("Great video");
        bookmark.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdBookmark = await bookmark.Content.ReadFromJsonAsync<BookmarkDto>(ApiTestClient.JsonOptions);
        createdBookmark!.TimestampSeconds.Should().Be(12);
        bookmarks.StatusCode.Should().Be(HttpStatusCode.OK);
        var bookmarkPage = await bookmarks.Content.ReadFromJsonAsync<PagedResponseDto<BookmarkDto>>(ApiTestClient.JsonOptions);
        bookmarkPage!.Items.Should().ContainSingle(item => item.Id == createdBookmark.Id);
    }

    [ApiIntegrationFact]
    public async Task AnalyticsEndpoint_ShouldRecordQualifiedPlayEvent()
    {
        await _fixture.ResetDatabaseAsync();
        var owner = await _fixture.CreateUserAsync(UserRole.Editor, "owner-analytics@example.com");
        var video = await SeedReadyVideoAsync(owner.User.Id, "Analytics Video");

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/v1/videos/{video.Id}/analytics/events",
            new RecordAnalyticsEventRequestDto(
                Guid.NewGuid(),
                AnalyticsEventType.Play,
                DateTime.UtcNow,
                Position: 0,
                DurationWatched: 5),
            ApiTestClient.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RecordAnalyticsEventResultDto>(ApiTestClient.JsonOptions);
        result!.EventRecorded.Should().BeTrue();
        result.ViewCountIncremented.Should().BeTrue();
    }

    private async Task<Video> SeedReadyVideoAsync(Guid ownerId, string title)
    {
        await using var context = _fixture.CreateContext();
        var category = context.Categories.OrderBy(category => category.DisplayOrder).FirstOrDefault();
        var video = Video.Create(title, "Seeded API test video", ownerId, category?.Id, VideoVisibility.Public, VideoStatus.Ready);
        context.Videos.Add(video);
        await context.SaveChangesAsync();
        return video;
    }
}
