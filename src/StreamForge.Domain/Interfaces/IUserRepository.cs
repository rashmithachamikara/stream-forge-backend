using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<PagedQueryResult<User>> SearchPagedAsync(
        string? searchTerm,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if email exists
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if username exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role
    /// </summary>
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}
