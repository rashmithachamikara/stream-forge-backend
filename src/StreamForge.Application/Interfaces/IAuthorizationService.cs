using StreamForge.Domain.Enums;

namespace StreamForge.Application.Interfaces;

public interface IAuthorizationService
{
    Task<bool> CanViewVideoAsync(
        Guid videoId,
        Guid? currentUserId,
        UserRole? currentUserRole = null,
        string? shareToken = null,
        CancellationToken cancellationToken = default);

    Task<bool> CanManageVideoAsync(
        Guid videoId,
        Guid currentUserId,
        UserRole? currentUserRole = null,
        CancellationToken cancellationToken = default);
}