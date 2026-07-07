using StreamForge.Domain.Enums;

namespace StreamForge.Application.Interfaces;

/// <summary>
/// Exposes the identity information resolved for the current request.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the authenticated user identifier when one is present.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the authenticated user's email address when available.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the authenticated user's display name when available.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the authenticated user's resolved role when available.
    /// </summary>
    UserRole? Role { get; }

    /// <summary>
    /// Gets whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
