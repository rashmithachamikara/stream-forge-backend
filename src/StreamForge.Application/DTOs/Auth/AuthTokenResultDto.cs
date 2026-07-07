namespace StreamForge.Application.DTOs.Auth;

/// <summary>
/// Represents issued token values and their expiry timestamps.
/// </summary>
public sealed record AuthTokenResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);
