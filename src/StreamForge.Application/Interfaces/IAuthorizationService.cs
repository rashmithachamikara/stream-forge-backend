using StreamForge.Domain.Enums;

namespace StreamForge.Application.Interfaces;

/// <summary>
/// Evaluates whether the current caller may view or manage video resources.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Determines whether the caller can view a video, optionally using share-token access.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="currentUserId">Authenticated user identifier, when available.</param>
    /// <param name="currentUserRole">Authenticated user role, when available.</param>
    /// <param name="shareToken">Optional share token used for anonymous or delegated access.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the caller may view the video; otherwise <see langword="false"/>.</returns>
    Task<bool> CanViewVideoAsync(
        Guid videoId,
        Guid? currentUserId,
        UserRole? currentUserRole = null,
        string? shareToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the caller can manage a video.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="currentUserId">Authenticated user identifier.</param>
    /// <param name="currentUserRole">Authenticated user role, when available.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the caller may manage the video; otherwise <see langword="false"/>.</returns>
    Task<bool> CanManageVideoAsync(
        Guid videoId,
        Guid currentUserId,
        UserRole? currentUserRole = null,
        CancellationToken cancellationToken = default);
}
