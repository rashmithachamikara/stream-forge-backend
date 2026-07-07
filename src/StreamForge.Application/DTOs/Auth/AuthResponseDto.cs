namespace StreamForge.Application.DTOs.Auth;

/// <summary>
/// Represents the authentication payload returned after successful registration, login, or token refresh.
/// </summary>
public sealed record AuthResponseDto(
    AuthUserDto User,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);
