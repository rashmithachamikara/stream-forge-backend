using FluentAssertions;
using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Tests;

public sealed class BookmarkTests
{
    [Fact]
    public void Create_ShouldTrimNoteAndStoreTimestamp()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        var bookmark = Bookmark.Create(userId, videoId, timestampSeconds: 42, note: "  intro note  ");

        bookmark.UserId.Should().Be(userId);
        bookmark.VideoId.Should().Be(videoId);
        bookmark.TimestampSeconds.Should().Be(42);
        bookmark.Note.Should().Be("intro note");
    }

    [Fact]
    public void Create_ShouldAllowMultipleBookmarksForSameUserAndVideo()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        var first = Bookmark.Create(userId, videoId, timestampSeconds: 10, note: "first");
        var second = Bookmark.Create(userId, videoId, timestampSeconds: 20, note: "second");

        first.Id.Should().NotBe(second.Id);
        first.UserId.Should().Be(second.UserId);
        first.VideoId.Should().Be(second.VideoId);
    }

    [Fact]
    public void Create_ShouldRejectNegativeTimestamp()
    {
        var act = () => Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), timestampSeconds: -1, note: null);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timestampSeconds");
    }

    [Fact]
    public void Update_ShouldNormalizeBlankNoteToNull()
    {
        var bookmark = Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), timestampSeconds: 10, note: "keep");

        bookmark.Update(timestampSeconds: 15, note: "   ");

        bookmark.TimestampSeconds.Should().Be(15);
        bookmark.Note.Should().BeNull();
    }
}
