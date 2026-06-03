using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for Tag entity
/// </summary>
public interface ITagRepository : IRepository<Tag>
{
    Task<PagedQueryResult<Tag>> SearchPagedAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tag by name
    /// </summary>
    Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tags by name pattern
    /// </summary>
    Task<IEnumerable<Tag>> SearchByNameAsync(string namePattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets most used tags
    /// </summary>
    Task<IEnumerable<Tag>> GetMostUsedAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if tag name exists
    /// </summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
