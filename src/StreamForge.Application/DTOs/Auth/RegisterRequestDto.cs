namespace StreamForge.Application.DTOs.Auth;

public sealed record RegisterRequestDto(
    string Name,
    string Email,
    string Password);