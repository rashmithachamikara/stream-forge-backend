using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

/// <summary>
/// Repository interface for Category entity
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Gets root categories (no parent)
    /// </summary>
    Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subcategories of a parent category
    /// </summary>
    Task<IEnumerable<Category>> GetSubcategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets category by slug
    /// </summary>
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if slug exists
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default);

    Task<bool> HasVideosAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task<bool> HasSubcategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
