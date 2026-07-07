using Microsoft.EntityFrameworkCore;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Enums;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Authentication;

public sealed class VideoAuthorizationService : IAuthorizationService
{
    private readonly StreamForgeDbContext _dbContext;

    public VideoAuthorizationService(StreamForgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanViewVideoAsync(
        Guid videoId,
        Guid? currentUserId,
        UserRole? currentUserRole = null,
        string? shareToken = null,
        CancellationToken cancellationToken = default)
    {
        if (currentUserRole == UserRole.Admin)
        {
            return true;
        }

        var video = await _dbContext.Videos
            .Include(candidate => candidate.AccessControls)
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == videoId, cancellationToken);

        if (video is null)
        {
            return false;
        }

        if (video.Visibility == VideoVisibility.Public)
        {
            return true;
        }

        if (currentUserId.HasValue && video.UploaderId == currentUserId.Value)
        {
            return true;
        }

        if (video.Visibility == VideoVisibility.Internal)
        {
            return currentUserId.HasValue;
        }

        return HasMatchingAccessGrant(video, currentUserId, shareToken);
    }

    public async Task<bool> CanManageVideoAsync(
        Guid videoId,
        Guid currentUserId,
        UserRole? currentUserRole = null,
        CancellationToken cancellationToken = default)
    {
        if (currentUserRole == UserRole.Admin)
        {
            return true;
        }

        return await _dbContext.Videos
            .AsNoTracking()
            .AnyAsync(candidate => candidate.Id == videoId && candidate.UploaderId == currentUserId, cancellationToken);
    }

    private static bool HasMatchingAccessGrant(
        Domain.Entities.Video video,
        Guid? currentUserId,
        string? shareToken)
    {
        var hasUserGrant = currentUserId.HasValue && video.AccessControls.Any(accessControl =>
            accessControl.IsActive &&
            !accessControl.IsExpired() &&
            accessControl.UserId == currentUserId.Value &&
            accessControl.PermissionType is PermissionType.View or PermissionType.Embed or PermissionType.Download);

        if (hasUserGrant)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(shareToken))
        {
            return false;
        }

        return video.AccessControls.Any(accessControl =>
            accessControl.IsActive &&
            !accessControl.IsExpired() &&
            accessControl.ShareToken == shareToken &&
            accessControl.PermissionType is PermissionType.View or PermissionType.Embed or PermissionType.Download);
    }
}
