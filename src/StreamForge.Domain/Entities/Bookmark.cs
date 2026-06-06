namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a user's personal timestamp bookmark within a video.
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

    /// <summary>
    /// Timestamp within the video in whole seconds.
    /// </summary>
    public int TimestampSeconds { get; private set; }

    /// <summary>
    /// Optional user note for the bookmark.
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

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
    public static Bookmark Create(Guid userId, Guid videoId, int timestampSeconds, string? note)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (timestampSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(timestampSeconds), "Timestamp must be zero or greater");

        var bookmark = new Bookmark
        {
            UserId = userId,
            VideoId = videoId,
            TimestampSeconds = timestampSeconds,
            Note = NormalizeNote(note),
            UpdatedAt = DateTime.UtcNow
        };

        return bookmark;
    }

    public void Update(int timestampSeconds, string? note)
    {
        if (timestampSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(timestampSeconds), "Timestamp must be zero or greater");

        TimestampSeconds = timestampSeconds;
        Note = NormalizeNote(note);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        return note.Trim();
    }
}
