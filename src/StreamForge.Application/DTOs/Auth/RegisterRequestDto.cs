namespace StreamForge.Application.DTOs.Auth;

/// <summary>
/// Request payload for creating a new user account.
/// </summary>
public sealed record RegisterRequestDto(
    string Name,
    string Email,
    string Password);
