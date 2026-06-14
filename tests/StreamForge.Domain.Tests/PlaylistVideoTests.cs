using FluentAssertions;
using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Tests;

public sealed class PlaylistVideoTests
{
    [Fact]
    public void Create_ShouldStoreOrdering()
    {
        var playlistId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        var item = PlaylistVideo.Create(playlistId, videoId, orderIndex: 3);

        item.PlaylistId.Should().Be(playlistId);
        item.VideoId.Should().Be(videoId);
        item.OrderIndex.Should().Be(3);
    }

    [Fact]
    public void UpdateOrderIndex_ShouldRejectNegativeIndex()
    {
        var item = PlaylistVideo.Create(Guid.NewGuid(), Guid.NewGuid(), orderIndex: 0);

        var act = () => item.UpdateOrderIndex(-1);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("orderIndex");
    }
}
