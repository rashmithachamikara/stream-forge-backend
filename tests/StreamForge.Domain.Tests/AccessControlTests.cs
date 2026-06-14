using FluentAssertions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Tests;

public sealed class AccessControlTests
{
    [Fact]
    public void CreateForUser_ShouldCreateActiveUserGrant()
    {
        var videoId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var grant = AccessControl.CreateForUser(videoId, userId, PermissionType.View);

        grant.VideoId.Should().Be(videoId);
        grant.UserId.Should().Be(userId);
        grant.ShareToken.Should().BeNull();
        grant.IsActive.Should().BeTrue();
        grant.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void CreateWithToken_ShouldCreateActiveTokenGrant()
    {
        var videoId = Guid.NewGuid();

        var grant = AccessControl.CreateWithToken(videoId, "share-token", PermissionType.View);

        grant.VideoId.Should().Be(videoId);
        grant.UserId.Should().BeNull();
        grant.ShareToken.Should().Be("share-token");
        grant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void DeactivateAndActivate_ShouldToggleActiveState()
    {
        var grant = AccessControl.CreateWithToken(Guid.NewGuid(), "share-token", PermissionType.View);

        grant.Deactivate();
        grant.Activate();

        grant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ExtendExpiration_ShouldRejectPastDate()
    {
        var grant = AccessControl.CreateWithToken(Guid.NewGuid(), "share-token", PermissionType.View);

        var act = () => grant.ExtendExpiration(DateTime.UtcNow.AddMinutes(-1));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("newExpiresAt");
    }
}
