using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Infrastructure.Persistence;
using StreamForge.Infrastructure.Persistence.Repositories;
using StreamForge.Infrastructure.Tests.Support;

namespace StreamForge.Infrastructure.Tests.Persistence;

[Collection(PostgresIntegrationCollection.Name)]
[Trait("Category", "PostgresIntegration")]
public sealed class RepositoryIntegrationTests
{
    private readonly PostgresIntegrationFixture _fixture;

    public RepositoryIntegrationTests(PostgresIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task VideoRepository_SearchVisible_ShouldApplyVisibilitySearchAndOrdering()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var viewer = User.Create("Viewer", "viewer@example.com", "hash", UserRole.Viewer);
        var category = Category.Create("Education");
        var tag = Tag.Create("backend");
        var publicVideo = Video.Create("Alpha PostgreSQL", "Public match", owner.Id, category.Id, VideoVisibility.Public, VideoStatus.Ready);
        var internalVideo = Video.Create("Beta PostgreSQL", "Internal match", owner.Id, category.Id, VideoVisibility.Internal, VideoStatus.Ready);
        var privateVideo = Video.Create("Gamma PostgreSQL", "Private match", owner.Id, category.Id, VideoVisibility.Private, VideoStatus.Ready);
        var processingVideo = Video.Create("Delta PostgreSQL", "Processing match", owner.Id, category.Id, VideoVisibility.Public, VideoStatus.Processing);
        context.AddRange(owner, viewer, category, tag, publicVideo, internalVideo, privateVideo, processingVideo);
        context.VideoTags.Add(VideoTag.Create(publicVideo.Id, tag.Id));
        context.AccessControls.Add(AccessControl.CreateForUser(privateVideo.Id, viewer.Id, PermissionType.View));
        await context.SaveChangesAsync();

        var repository = new VideoRepository(context);

        var anonymous = await repository.SearchVisibleAsync(
            "postgresql",
            category.Id,
            tagId: null,
            uploaderId: null,
            status: null,
            visibility: null,
            currentUserId: null,
            currentUserRole: null,
            sort: "title",
            page: 1,
            pageSize: 10);
        var authenticated = await repository.SearchVisibleAsync(
            "postgresql",
            category.Id,
            tagId: null,
            uploaderId: null,
            status: null,
            visibility: null,
            currentUserId: viewer.Id,
            currentUserRole: UserRole.Viewer,
            sort: "title",
            page: 1,
            pageSize: 10);
        var byTag = await repository.SearchVisibleAsync(
            null,
            categoryId: null,
            tag.Id,
            uploaderId: null,
            status: null,
            visibility: null,
            currentUserId: viewer.Id,
            currentUserRole: UserRole.Viewer,
            sort: "title",
            page: 1,
            pageSize: 10);

        anonymous.Items.Select(video => video.Id).Should().Equal(publicVideo.Id);
        authenticated.Items.Select(video => video.Id).Should().Equal(publicVideo.Id, internalVideo.Id, privateVideo.Id);
        authenticated.Items.Should().NotContain(video => video.Id == processingVideo.Id);
        byTag.Items.Should().ContainSingle(video => video.Id == publicVideo.Id);
    }

    [IntegrationFact]
    public async Task BookmarkRepository_ShouldAllowMultipleMarkersAndOrderVideoBookmarksByTimestamp()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var user = User.Create("Viewer", "viewer@example.com", "hash", UserRole.Viewer);
        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var video = Video.Create("Video", null, owner.Id, status: VideoStatus.Ready);
        var later = Bookmark.Create(user.Id, video.Id, 90, "later");
        var earlier = Bookmark.Create(user.Id, video.Id, 10, "earlier");
        var middle = Bookmark.Create(user.Id, video.Id, 45, "middle");
        context.AddRange(user, owner, video, later, earlier, middle);
        await context.SaveChangesAsync();

        var repository = new BookmarkRepository(context);

        var result = await repository.GetPagedByVideoIdAsync(user.Id, video.Id, page: 1, pageSize: 2);

        result.TotalCount.Should().Be(3);
        result.Items.Select(bookmark => bookmark.TimestampSeconds).Should().Equal(10, 45);
    }

    [IntegrationFact]
    public async Task CategoryAndTagRepositories_ShouldUseRelationalGuardQueries()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var parent = Category.Create("Education");
        var child = Category.Create("Programming", parentCategoryId: parent.Id);
        var tag = Tag.Create("Backend");
        var video = Video.Create("Video", null, owner.Id, child.Id, VideoVisibility.Public, VideoStatus.Ready);
        context.AddRange(owner, parent, child, tag, video);
        context.VideoTags.Add(VideoTag.Create(video.Id, tag.Id));
        await context.SaveChangesAsync();

        var categories = new CategoryRepository(context);
        var tags = new TagRepository(context);

        (await categories.NameExistsAsync(" Education ")).Should().BeTrue();
        (await categories.HasSubcategoriesAsync(parent.Id)).Should().BeTrue();
        (await categories.HasVideosAsync(child.Id)).Should().BeTrue();
        (await tags.NameExistsAsync(" backend ")).Should().BeTrue();
        (await tags.IsInUseAsync(tag.Id)).Should().BeTrue();
    }

    [IntegrationFact]
    public async Task PlaylistVideoRepository_ShouldReturnDeterministicOrderAndEnforceUniquePlaylistVideo()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var playlist = Playlist.Create("Playlist", owner.Id, null, PlaylistVisibility.Private);
        var firstVideo = Video.Create("First", null, owner.Id, status: VideoStatus.Ready);
        var secondVideo = Video.Create("Second", null, owner.Id, status: VideoStatus.Ready);
        context.AddRange(owner, playlist, firstVideo, secondVideo);
        context.PlaylistVideos.Add(PlaylistVideo.Create(playlist.Id, secondVideo.Id, orderIndex: 1));
        context.PlaylistVideos.Add(PlaylistVideo.Create(playlist.Id, firstVideo.Id, orderIndex: 0));
        await context.SaveChangesAsync();
        var repository = new PlaylistVideoRepository(context);

        var items = await repository.GetByPlaylistIdAsync(playlist.Id);

        items.Select(item => item.VideoId).Should().Equal(firstVideo.Id, secondVideo.Id);
        await using var duplicateContext = _fixture.CreateContext();
        duplicateContext.PlaylistVideos.Add(PlaylistVideo.Create(playlist.Id, firstVideo.Id, orderIndex: 2));
        var duplicateSave = () => duplicateContext.SaveChangesAsync();
        await duplicateSave.Should().ThrowAsync<DbUpdateException>();
    }

    [IntegrationFact]
    public async Task UnitOfWork_ShouldPersistPlaylistVideoCountAfterAddAndRemove()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var owner = User.Create("Owner", "owner@example.com", "hash", UserRole.Editor);
        var playlist = Playlist.Create("Playlist", owner.Id, null, PlaylistVisibility.Private);
        var video = Video.Create("Video", null, owner.Id, status: VideoStatus.Ready);
        context.AddRange(owner, playlist, video);
        await context.SaveChangesAsync();
        await using var addContext = _fixture.CreateContext();
        using (var addUnitOfWork = new UnitOfWork(addContext))
        {
            var trackedPlaylist = await addUnitOfWork.Playlists.GetByIdAsync(playlist.Id);
            trackedPlaylist!.IncrementVideoCount();
            await addUnitOfWork.PlaylistVideos.AddAsync(PlaylistVideo.Create(playlist.Id, video.Id, 0));
            await addUnitOfWork.SaveChangesAsync();
        }

        await using var verifyAddContext = _fixture.CreateContext();
        var afterAdd = await verifyAddContext.Playlists.SingleAsync(item => item.Id == playlist.Id);
        afterAdd.VideoCount.Should().Be(1);

        await using var removeContext = _fixture.CreateContext();
        using (var removeUnitOfWork = new UnitOfWork(removeContext))
        {
            var trackedPlaylist = await removeUnitOfWork.Playlists.GetByIdAsync(playlist.Id);
            var playlistVideo = await removeUnitOfWork.PlaylistVideos.GetByPlaylistAndVideoAsync(playlist.Id, video.Id);
            trackedPlaylist!.DecrementVideoCount();
            await removeUnitOfWork.PlaylistVideos.DeleteAsync(playlistVideo!);
            await removeUnitOfWork.SaveChangesAsync();
        }

        await using var verifyRemoveContext = _fixture.CreateContext();
        var afterRemove = await verifyRemoveContext.Playlists.SingleAsync(item => item.Id == playlist.Id);
        afterRemove.VideoCount.Should().Be(0);
        (await verifyRemoveContext.PlaylistVideos.ToListAsync()).Should().BeEmpty();
    }
}
