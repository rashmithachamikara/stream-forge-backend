namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between Videos and Tags
/// </summary>
public class VideoTag
{
    /// <summary>
    /// Video ID
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// Tag ID
    /// </summary>
    public Guid TagId { get; private set; }

    /// <summary>
    /// When the tag was added to the video
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Video Video { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    // Private constructor for EF Core
    private VideoTag()
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new video-tag association
    /// </summary>
    public static VideoTag Create(Guid videoId, Guid tagId)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (tagId == Guid.Empty)
            throw new ArgumentException("Tag ID is required", nameof(tagId));

        var videoTag = new VideoTag
        {
            VideoId = videoId,
            TagId = tagId,
            CreatedAt = DateTime.UtcNow
        };

        return videoTag;
    }
}
