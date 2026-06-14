using FluentAssertions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Tests;

public sealed class VideoReactionTests
{
    [Fact]
    public void Create_ShouldStoreReaction()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        var reaction = VideoReaction.Create(userId, videoId, ReactionType.Like);

        reaction.UserId.Should().Be(userId);
        reaction.VideoId.Should().Be(videoId);
        reaction.ReactionType.Should().Be(ReactionType.Like);
    }

    [Fact]
    public void ChangeReaction_ShouldReplaceReactionType()
    {
        var reaction = VideoReaction.Create(Guid.NewGuid(), Guid.NewGuid(), ReactionType.Like);

        reaction.ChangeReaction(ReactionType.Dislike);

        reaction.ReactionType.Should().Be(ReactionType.Dislike);
    }

    [Fact]
    public void Create_ShouldRejectMissingUserId()
    {
        var act = () => VideoReaction.Create(Guid.Empty, Guid.NewGuid(), ReactionType.Like);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("userId");
    }
}
