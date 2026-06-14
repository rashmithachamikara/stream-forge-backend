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

    private static StreamForgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StreamForgeDbContext>()
            .UseNpgsql("Host=localhost;Database=streamforge_model_only;Username=postgres;Password=postgres")
            .Options;

        return new StreamForgeDbContext(options);
    }
}
