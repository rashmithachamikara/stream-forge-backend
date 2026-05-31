namespace StreamForge.Application.DTOs.Auth;

public sealed record AuthResponseDto(
    AuthUserDto User,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);