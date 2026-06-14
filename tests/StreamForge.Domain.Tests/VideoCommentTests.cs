using FluentAssertions;
using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Tests;

public sealed class VideoCommentTests
{
    [Fact]
    public void Create_ShouldSupportReplies()
    {
        var parentCommentId = Guid.NewGuid();

        var comment = VideoComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Reply", parentCommentId);

        comment.ParentCommentId.Should().Be(parentCommentId);
        comment.Comment.Should().Be("Reply");
        comment.IsEdited.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldMarkCommentEdited()
    {
        var comment = VideoComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Original");

        comment.Update("Updated");

        comment.Comment.Should().Be("Updated");
        comment.IsEdited.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRejectBlankComment()
    {
        var act = () => VideoComment.Create(Guid.NewGuid(), Guid.NewGuid(), " ");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("comment");
    }
}
