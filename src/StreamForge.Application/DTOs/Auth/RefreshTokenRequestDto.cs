namespace StreamForge.Application.DTOs.Auth;

/// <summary>
/// Request payload for exchanging a refresh token for a new session.
/// </summary>
public sealed record RefreshTokenRequestDto(
    string RefreshToken);
