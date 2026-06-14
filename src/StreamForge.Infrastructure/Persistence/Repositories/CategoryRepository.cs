using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : BaseRepository<Category>, ICategoryRepository
{
    public CategoryRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default) =>
        await DbSet.Where(category => category.ParentCategoryId == null).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Category>> GetSubcategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(category => category.ParentCategoryId == parentCategoryId).ToListAsync(cancellationToken);

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(category => category.Name == slug, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(category => category.Name == slug, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        var query = DbSet.Where(category => category.Name == trimmedName);
        if (excludeCategoryId.HasValue)
        {
            query = query.Where(category => category.Id != excludeCategoryId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasVideosAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        DbContext.Videos.AnyAsync(video => video.CategoryId == categoryId, cancellationToken);

    public Task<bool> HasSubcategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(category => category.ParentCategoryId == categoryId, cancellationToken);
}
