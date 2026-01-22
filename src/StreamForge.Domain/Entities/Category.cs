namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a video category with hierarchical support
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Category description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Parent category ID for hierarchical structure
    /// </summary>
    public Guid? ParentCategoryId { get; private set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; private set; }

    // Navigation properties
    public Category? ParentCategory { get; private set; }
    public ICollection<Category> SubCategories { get; private set; }
    public ICollection<Video> Videos { get; private set; }

    // Private constructor for EF Core
    private Category() : base()
    {
        Name = string.Empty;
        SubCategories = new List<Category>();
        Videos = new List<Video>();
    }

    /// <summary>
    /// Creates a new category
    /// </summary>
    public static Category Create(string name, string? description = null, Guid? parentCategoryId = null, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));

        var category = new Category
        {
            Name = name,
            Description = description,
            ParentCategoryId = parentCategoryId,
            DisplayOrder = displayOrder
        };

        return category;
    }

    /// <summary>
    /// Updates category information
    /// </summary>
    public void Update(string name, string? description, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Sets parent category
    /// </summary>
    public void SetParent(Guid? parentCategoryId)
    {
        if (parentCategoryId.HasValue && parentCategoryId.Value == Id)
            throw new InvalidOperationException("Category cannot be its own parent");

        ParentCategoryId = parentCategoryId;
    }
}
