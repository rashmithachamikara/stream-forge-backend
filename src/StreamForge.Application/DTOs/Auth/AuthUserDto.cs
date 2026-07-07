using StreamForge.Domain.Enums;

namespace StreamForge.Application.DTOs.Auth;

/// <summary>
/// Represents the authenticated user profile returned by auth endpoints.
/// </summary>
public sealed record AuthUserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role);
