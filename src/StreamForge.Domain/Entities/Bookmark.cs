namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a user's bookmarked video
/// </summary>
public class Bookmark : BaseEntity
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Video ID
    /// </summary>
    public Guid VideoId { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public Video Video { get; private set; } = null!;

    // Private constructor for EF Core
    private Bookmark() : base()
    {
    }

    /// <summary>
    /// Creates a new bookmark
    /// </summary>
    public static Bookmark Create(Guid userId, Guid videoId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        var bookmark = new Bookmark
        {
            UserId = userId,
            VideoId = videoId
        };

        return bookmark;
    }
}
