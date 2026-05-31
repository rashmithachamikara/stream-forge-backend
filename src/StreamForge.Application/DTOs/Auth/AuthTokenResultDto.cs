namespace StreamForge.Application.DTOs.Auth;

public sealed record AuthTokenResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);