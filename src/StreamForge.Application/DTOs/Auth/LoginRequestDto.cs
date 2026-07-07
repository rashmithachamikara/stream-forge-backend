namespace StreamForge.Application.DTOs.Auth;

/// <summary>
/// Request payload for authenticating an existing user.
/// </summary>
public sealed record LoginRequestDto(
    string Email,
    string Password);
