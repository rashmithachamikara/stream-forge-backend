using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StreamForge.Api.IntegrationTests.Support;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using static StreamForge.Api.IntegrationTests.Support.PlaybackTestMedia;

namespace StreamForge.Api.IntegrationTests.Endpoints;

[Collection(ApiIntegrationCollection.Name)]
[Trait("Category", "ApiIntegration")]
public sealed class VideoPlaybackEndpointTests
{
    private readonly ApiIntegrationFixture _fixture;

    public VideoPlaybackEndpointTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [ApiIntegrationFact]
    public async Task PublicPlayback_ShouldServeMediaWithoutCredentialsAndSupportSegmentRanges()
    {
        await _fixture.ResetDatabaseAsync();
        var owner = await _fixture.CreateUserAsync(UserRole.Editor, "playback-owner@example.com");
        var videoId = await SeedReadyVideoAsync(owner.User.Id, VideoVisibility.Public);

        await AssertPlaybackAsync(videoId);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/videos/{videoId}/playback/assets/{SegmentPath}");
        request.Headers.Range = new RangeHeaderValue(1, 3);
        using var response = await _fixture.Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Content.Headers.ContentRange.Should().Be(new ContentRangeHeaderValue(1, 3, SegmentBytes.Length));
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(SegmentBytes[1..4]);
    }

    [ApiIntegrationFact]
    public async Task PrivatePlayback_ShouldAllowOwnerAndRejectViewersWithoutAccess()
    {
        await _fixture.ResetDatabaseAsync();
        var owner = await _fixture.CreateUserAsync(UserRole.Editor, "playback-owner@example.com");
        var viewer = await _fixture.CreateUserAsync(UserRole.Viewer, "playback-viewer@example.com");
        var videoId = await SeedReadyVideoAsync(owner.User.Id, VideoVisibility.Private);

        await AssertPlaybackAsync(videoId, accessToken: owner.AccessToken);
        await AssertPlaybackDeniedAsync(videoId);
        await AssertPlaybackDeniedAsync(videoId, accessToken: viewer.AccessToken);
    }

    [ApiIntegrationFact]
    public async Task SharedPlayback_ShouldAllowValidTokenAndRejectInvalidOrExpiredTokens()
    {
        await _fixture.ResetDatabaseAsync();
        var owner = await _fixture.CreateUserAsync(UserRole.Editor, "playback-owner@example.com");
        var videoId = await SeedReadyVideoAsync(owner.User.Id, VideoVisibility.Private);
        const string validToken = "playback+share/token=";
        const string expiredToken = "expired-playback-token";

        await using (var context = _fixture.CreateContext())
        {
            context.AccessControls.AddRange(
                AccessControl.CreateWithToken(videoId, validToken, PermissionType.View),
                AccessControl.CreateWithToken(videoId, expiredToken, PermissionType.View, DateTime.UnixEpoch));
            await context.SaveChangesAsync();
        }

        await AssertPlaybackAsync(videoId, shareToken: validToken);
        await AssertPlaybackDeniedAsync(videoId, shareToken: "invalid-playback-token");
        await AssertPlaybackDeniedAsync(videoId, shareToken: expiredToken);
    }

    private async Task AssertPlaybackAsync(Guid videoId, string? accessToken = null, string? shareToken = null)
    {
        var videoUrl = $"/api/v1/videos/{videoId}";
        var shareQuery = shareToken is null ? string.Empty : $"?shareToken={Uri.EscapeDataString(shareToken)}";
        var playlistReference = $"assets/{PlaylistPath}{shareQuery}";

        using var manifest = await GetAsync($"{videoUrl}/playback/manifest{shareQuery}", accessToken);
        manifest.StatusCode.Should().Be(HttpStatusCode.OK);
        (manifest.Content.Headers.ContentType?.MediaType).Should().Be(HlsContentType);
        (await manifest.Content.ReadAsStringAsync()).Should().Be($"{MasterHeader}{playlistReference}\n");

        using var playlist = await GetAsync($"{videoUrl}/playback/{playlistReference}", accessToken);
        playlist.StatusCode.Should().Be(HttpStatusCode.OK);
        (playlist.Content.Headers.ContentType?.MediaType).Should().Be(HlsContentType);
        (await playlist.Content.ReadAsStringAsync()).Should().Be(VariantPlaylist);

        using var segment = await GetAsync($"{videoUrl}/playback/assets/{SegmentPath}{shareQuery}", accessToken);
        segment.StatusCode.Should().Be(HttpStatusCode.OK);
        (segment.Content.Headers.ContentType?.MediaType).Should().Be("video/mp2t");
        (await segment.Content.ReadAsByteArrayAsync()).Should().Equal(SegmentBytes);

        using var thumbnail = await GetAsync($"{videoUrl}/thumbnail{shareQuery}", accessToken);
        thumbnail.StatusCode.Should().Be(HttpStatusCode.OK);
        (thumbnail.Content.Headers.ContentType?.MediaType).Should().Be("image/jpeg");
        (await thumbnail.Content.ReadAsByteArrayAsync()).Should().Equal(ThumbnailBytes);
    }

    private async Task AssertPlaybackDeniedAsync(Guid videoId, string? accessToken = null, string? shareToken = null)
    {
        var shareQuery = shareToken is null ? string.Empty : $"?shareToken={Uri.EscapeDataString(shareToken)}";
        string[] paths = ["playback/manifest", $"playback/assets/{PlaylistPath}", $"playback/assets/{SegmentPath}", "thumbnail"];
        foreach (var path in paths)
        {
            using var response = await GetAsync($"/api/v1/videos/{videoId}/{path}{shareQuery}", accessToken);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"the caller has no access to {path}");
        }
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string? accessToken)
    {
        using var request = ApiTestClient.JsonRequest(HttpMethod.Get, url, accessToken: accessToken);
        return await _fixture.Client.SendAsync(request);
    }

    private async Task<Guid> SeedReadyVideoAsync(Guid ownerId, VideoVisibility visibility)
    {
        var video = Video.Create("Playback integration test", null, ownerId, visibility: visibility, status: VideoStatus.Ready);

        using var scope = _fixture.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var hls = await WriteHlsAsync(storage, video.Id);
        var thumbnail = await WriteThumbnailAsync(storage, video.Id);

        await using var context = _fixture.CreateContext();
        context.Videos.Add(video);
        context.VideoVersions.Add(VideoVersion.Create(video.Id, "adaptive", VideoFormat.HLS,
            hls.MasterPlaylistPath, hls.MasterPlaylistSize, Metadata.DurationSeconds));
        context.VideoThumbnails.Add(VideoThumbnail.Create(video.Id, thumbnail.StoragePath, thumbnail.Width,
            thumbnail.Height, thumbnail.SizeBytes, isDefault: true));
        await context.SaveChangesAsync();
        return video.Id;
    }
}
