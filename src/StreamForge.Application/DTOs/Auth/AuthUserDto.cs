using StreamForge.Domain.Enums;

namespace StreamForge.Application.DTOs.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role);