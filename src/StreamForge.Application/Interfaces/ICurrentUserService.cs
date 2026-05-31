using StreamForge.Domain.Enums;

namespace StreamForge.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? Name { get; }

    UserRole? Role { get; }

    bool IsAuthenticated { get; }
}