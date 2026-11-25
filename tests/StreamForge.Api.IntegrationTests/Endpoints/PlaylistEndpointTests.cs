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
public sealed class PlaylistEndpointTests(ApiIntegrationFixture fixture)
{
    private const string _playlistsUrl = "/api/v1/playlists";
    private const string _myPlaylistsUrl = "/api/v1/me/playlists";

    [ApiIntegrationFact]
    public async Task Owner_ShouldManagePlaylistAndVideoOrder()
    {
        await fixture.ResetDatabaseAsync();
        var owner = await fixture.CreateUserAsync(UserRole.Editor, "playlist-owner@example.com");
        var client = fixture.Client;
        var firstVideoId = await SeedVideoAsync(owner.User.Id, "First video");
        var secondVideoId = await SeedVideoAsync(owner.User.Id, "Second video");

        var playlist = await client.ReadOkJsonAsync<PlaylistDto>(HttpMethod.Post, _playlistsUrl, owner.AccessToken,
            new CreatePlaylistRequestDto("My playlist", null, PlaylistVisibility.Private));
        playlist.Id.Should().NotBeEmpty();
        playlist.OwnerId.Should().Be(owner.User.Id);
        playlist.Visibility.Should().Be(PlaylistVisibility.Private);
        playlist.VideoCount.Should().Be(0);

        var mine = await client.ReadOkJsonAsync<PagedResponseDto<PlaylistDto>>(HttpMethod.Get, _myPlaylistsUrl, owner.AccessToken);
        mine.Items.Select(item => item.Id).Should().Equal(playlist.Id);

        var playlistUrl = $"{_playlistsUrl}/{playlist.Id}";
        var videosUrl = $"{playlistUrl}/videos";
        var updated = await client.ReadOkJsonAsync<PlaylistDto>(HttpMethod.Patch, playlistUrl, owner.AccessToken,
            new UpdatePlaylistRequestDto("Updated playlist", "A public playlist", PlaylistVisibility.Public));
        updated.Name.Should().Be("Updated playlist");
        updated.Description.Should().Be("A public playlist");
        updated.Visibility.Should().Be(PlaylistVisibility.Public);

        var publicList = await client.ReadOkJsonAsync<PagedResponseDto<PlaylistDto>>(HttpMethod.Get, $"{_playlistsUrl}?ownerId={owner.User.Id}");
        publicList.Items.Select(item => item.Id).Should().Equal(playlist.Id);
        var publicDetails = await client.ReadOkJsonAsync<PlaylistDto>(HttpMethod.Get, playlistUrl);
        publicDetails.Name.Should().Be(updated.Name);
        publicDetails.Description.Should().Be(updated.Description);
        publicDetails.Visibility.Should().Be(PlaylistVisibility.Public);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, videosUrl, owner.AccessToken,
            new AddPlaylistVideoRequestDto(firstVideoId, null));
        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, videosUrl, owner.AccessToken,
            new AddPlaylistVideoRequestDto(secondVideoId, null));
        var added = await client.ReadOkJsonAsync<PlaylistVideosPageDto>(HttpMethod.Get, videosUrl);
        added.Playlist.VideoCount.Should().Be(2);
        added.Videos.Items.Select(item => item.Id).Should().Equal(firstVideoId, secondVideoId);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, $"{videosUrl}/reorder", owner.AccessToken,
            new ReorderPlaylistVideosRequestDto([secondVideoId, firstVideoId]));
        var reordered = await client.ReadOkJsonAsync<PlaylistVideosPageDto>(HttpMethod.Get, videosUrl);
        reordered.Videos.Items.Select(item => item.Id).Should().Equal(secondVideoId, firstVideoId);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Delete, $"{videosUrl}/{secondVideoId}", owner.AccessToken);
        var remaining = await client.ReadOkJsonAsync<PlaylistVideosPageDto>(HttpMethod.Get, videosUrl);
        remaining.Playlist.VideoCount.Should().Be(1);
        remaining.Videos.Items.Select(item => item.Id).Should().Equal(firstVideoId);

        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Delete, playlistUrl, owner.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.NotFound, HttpMethod.Get, playlistUrl, owner.AccessToken);
    }

    [ApiIntegrationFact]
    public async Task PrivatePlaylist_ShouldRejectUnrelatedViewersAndTheirChanges()
    {
        await fixture.ResetDatabaseAsync();
        var owner = await fixture.CreateUserAsync(UserRole.Editor, "playlist-owner@example.com");
        var viewer = await fixture.CreateUserAsync(UserRole.Viewer, "playlist-viewer@example.com");
        var client = fixture.Client;
        var videoId = await SeedVideoAsync(owner.User.Id, "Public video in a private playlist");
        var secondVideoId = await SeedVideoAsync(owner.User.Id, "Another public video");
        var playlist = await client.ReadOkJsonAsync<PlaylistDto>(HttpMethod.Post, _playlistsUrl, owner.AccessToken,
            new CreatePlaylistRequestDto("Private playlist", null, PlaylistVisibility.Private));
        var playlistUrl = $"{_playlistsUrl}/{playlist.Id}";
        var videosUrl = $"{playlistUrl}/videos";
        await client.AssertStatusAsync(HttpStatusCode.NoContent, HttpMethod.Post, videosUrl, owner.AccessToken,
            new AddPlaylistVideoRequestDto(videoId, null));

        var privateList = await client.ReadOkJsonAsync<PagedResponseDto<PlaylistDto>>(
            HttpMethod.Get, $"{_playlistsUrl}?ownerId={owner.User.Id}", viewer.AccessToken);
        privateList.Items.Should().BeEmpty();
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, playlistUrl);
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, playlistUrl, viewer.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Get, videosUrl, viewer.AccessToken);

        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Patch, playlistUrl, viewer.AccessToken,
            new UpdatePlaylistRequestDto("Changed by viewer", null, PlaylistVisibility.Public));
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Post, videosUrl, viewer.AccessToken,
            new AddPlaylistVideoRequestDto(secondVideoId, null));
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Post, $"{videosUrl}/reorder", viewer.AccessToken,
            new ReorderPlaylistVideosRequestDto([videoId]));
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Delete, $"{videosUrl}/{videoId}", viewer.AccessToken);
        await client.AssertStatusAsync(HttpStatusCode.Unauthorized, HttpMethod.Delete, playlistUrl, viewer.AccessToken);

        var ownerView = await client.ReadOkJsonAsync<PlaylistVideosPageDto>(HttpMethod.Get, videosUrl, owner.AccessToken);
        ownerView.Playlist.Name.Should().Be(playlist.Name);
        ownerView.Playlist.Visibility.Should().Be(PlaylistVisibility.Private);
        ownerView.Videos.Items.Select(item => item.Id).Should().Equal(videoId);
    }

    private async Task<Guid> SeedVideoAsync(Guid ownerId, string title)
    {
        await using var context = fixture.CreateContext();
        var video = Video.Create(title, null, ownerId, visibility: VideoVisibility.Public, status: VideoStatus.Ready);
        context.Videos.Add(video);
        await context.SaveChangesAsync();
        return video.Id;
    }
}
