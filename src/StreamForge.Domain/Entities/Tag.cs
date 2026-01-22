namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a video tag
/// </summary>
public class Tag : BaseEntity
{
    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Number of videos using this tag (denormalized for performance)
    /// </summary>
    public int UsageCount { get; private set; }

    // Navigation properties
    public ICollection<VideoTag> VideoTags { get; private set; }

    // Private constructor for EF Core
    private Tag() : base()
    {
        Name = string.Empty;
        VideoTags = new List<VideoTag>();
    }

    /// <summary>
    /// Creates a new tag
    /// </summary>
    public static Tag Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));

        var tag = new Tag
        {
            Name = name.Trim().ToLowerInvariant(),
            UsageCount = 0
        };

        return tag;
    }

    /// <summary>
    /// Increments usage count
    /// </summary>
    public void IncrementUsageCount()
    {
        UsageCount++;
    }

    /// <summary>
    /// Decrements usage count
    /// </summary>
    public void DecrementUsageCount()
    {
        if (UsageCount > 0)
            UsageCount--;
    }

    /// <summary>
    /// Updates tag name
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));

        Name = name.Trim().ToLowerInvariant();
    }
}
