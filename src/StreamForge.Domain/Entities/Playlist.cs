using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a user-created playlist
/// </summary>
public class Playlist : BaseEntity
{
    /// <summary>
    /// Playlist name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Playlist description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Owner user ID
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    /// Playlist visibility
    /// </summary>
    public PlaylistVisibility Visibility { get; private set; }

    /// <summary>
    /// Number of videos in playlist (denormalized for performance)
    /// </summary>
    public int VideoCount { get; private set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public User Owner { get; private set; } = null!;
    public ICollection<PlaylistVideo> PlaylistVideos { get; private set; }

    // Private constructor for EF Core
    private Playlist() : base()
    {
        Name = string.Empty;
        PlaylistVideos = new List<PlaylistVideo>();
    }

    /// <summary>
    /// Creates a new playlist
    /// </summary>
    public static Playlist Create(string name, Guid ownerId, string? description = null, PlaylistVisibility visibility = PlaylistVisibility.Private)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Playlist name cannot be empty", nameof(name));

        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner ID is required", nameof(ownerId));

        var playlist = new Playlist
        {
            Name = name,
            Description = description,
            OwnerId = ownerId,
            Visibility = visibility,
            VideoCount = 0,
            UpdatedAt = DateTime.UtcNow
        };

        return playlist;
    }

    /// <summary>
    /// Updates playlist information
    /// </summary>
    public void Update(string name, string? description, PlaylistVisibility visibility)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Playlist name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        Visibility = visibility;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments video count
    /// </summary>
    public void IncrementVideoCount()
    {
        VideoCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Decrements video count
    /// </summary>
    public void DecrementVideoCount()
    {
        if (VideoCount > 0)
        {
            VideoCount--;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
