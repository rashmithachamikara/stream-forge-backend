using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Tests.Data;

public sealed class EfModelConfigurationTests
{
    [Fact]
    public void BookmarkConfiguration_ShouldAllowMultipleBookmarksPerUserAndVideo()
    {
        using var context = CreateContext();

        var bookmarkEntity = context.Model.FindEntityType(typeof(Bookmark));

        bookmarkEntity.Should().NotBeNull();
        bookmarkEntity!.GetIndexes()
            .FirstOrDefault(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Bookmark.UserId), nameof(Bookmark.VideoId) }))
            ?.IsUnique
            .Should()
            .NotBe(true, "timestamp bookmarks allow multiple markers for the same user/video");
    }

    [Fact]
    public void VideoReactionConfiguration_ShouldKeepOneReactionPerUserAndVideo()
    {
        using var context = CreateContext();

        var reactionEntity = context.Model.FindEntityType(typeof(VideoReaction));
        var uniqueIndex = reactionEntity!.GetIndexes()
            .FirstOrDefault(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(VideoReaction.VideoId), nameof(VideoReaction.UserId) }));

        uniqueIndex.Should().NotBeNull();
        uniqueIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void TagConfiguration_ShouldKeepTagNamesUnique()
    {
        using var context = CreateContext();

        var tagEntity = context.Model.FindEntityType(typeof(Tag));
        var nameIndex = tagEntity!.GetIndexes()
            .FirstOrDefault(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Tag.Name) }));

        nameIndex.Should().NotBeNull();
        nameIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void PlaylistVideoConfiguration_ShouldKeepOneVideoPerPlaylist()
    {
        using var context = CreateContext();

        var playlistVideoEntity = context.Model.FindEntityType(typeof(PlaylistVideo));
        var uniqueIndex = playlistVideoEntity!.GetIndexes()
            .FirstOrDefault(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(PlaylistVideo.PlaylistId), nameof(PlaylistVideo.VideoId) }));

        uniqueIndex.Should().NotBeNull();
        uniqueIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void VideoTranscriptChunkConfiguration_ShouldRegisterFullTextAndTrigramSearchArtifacts()
    {
        using var context = CreateContext();

        var chunkEntity = context.Model.FindEntityType(typeof(VideoTranscriptChunk));
        chunkEntity.Should().NotBeNull();

        var searchVectorIndex = chunkEntity!.GetIndexes()
            .FirstOrDefault(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "SearchVector" }));
        searchVectorIndex.Should().NotBeNull();
        searchVectorIndex!.FindAnnotation("Npgsql:IndexMethod")?.Value.Should().Be("GIN");

        var contentTrigramIndex = chunkEntity.GetIndexes()
            .FirstOrDefault(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(VideoTranscriptChunk.Content) }));
        contentTrigramIndex.Should().NotBeNull();
        contentTrigramIndex!.GetDatabaseName().Should().Be("IX_VideoTranscriptChunks_Content_Trgm");
        contentTrigramIndex!.FindAnnotation("Npgsql:IndexMethod")?.Value.Should().Be("GIN");
    }

    private static StreamForgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StreamForgeDbContext>()
            .UseNpgsql("Host=localhost;Database=streamforge_model_only;Username=postgres;Password=postgres")
            .Options;

        return new StreamForgeDbContext(options);
    }
}
