using FluentAssertions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Tests;

public sealed class PlaylistTests
{
    [Fact]
    public void Create_ShouldStartWithZeroVideos()
    {
        var ownerId = Guid.NewGuid();

        var playlist = Playlist.Create("Watch Later", ownerId, "private queue", PlaylistVisibility.Private);

        playlist.Name.Should().Be("Watch Later");
        playlist.OwnerId.Should().Be(ownerId);
        playlist.Description.Should().Be("private queue");
        playlist.Visibility.Should().Be(PlaylistVisibility.Private);
        playlist.VideoCount.Should().Be(0);
    }

    [Fact]
    public void IncrementAndDecrementVideoCount_ShouldKeepCountNonNegative()
    {
        var playlist = Playlist.Create("Favorites", Guid.NewGuid());

        playlist.IncrementVideoCount();
        playlist.IncrementVideoCount();
        playlist.DecrementVideoCount();
        playlist.DecrementVideoCount();
        playlist.DecrementVideoCount();

        playlist.VideoCount.Should().Be(0);
    }

    [Fact]
    public void Update_ShouldRejectBlankName()
    {
        var playlist = Playlist.Create("Favorites", Guid.NewGuid());

        var act = () => playlist.Update(" ", null, PlaylistVisibility.Public);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }
}
