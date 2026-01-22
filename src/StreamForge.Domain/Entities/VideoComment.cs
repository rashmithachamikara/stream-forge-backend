namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a user comment on a video with reply support
/// </summary>
public class VideoComment : BaseEntity
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
    /// Parent comment ID for replies (null for top-level comments)
    /// </summary>
    public Guid? ParentCommentId { get; private set; }

    /// <summary>
    /// Comment text
    /// </summary>
    public string Comment { get; private set; }

    /// <summary>
    /// Whether the comment has been edited
    /// </summary>
    public bool IsEdited { get; private set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public Video Video { get; private set; } = null!;
    public VideoComment? ParentComment { get; private set; }
    public ICollection<VideoComment> Replies { get; private set; }

    // Private constructor for EF Core
    private VideoComment() : base()
    {
        Comment = string.Empty;
        Replies = new List<VideoComment>();
    }

    /// <summary>
    /// Creates a new comment
    /// </summary>
    public static VideoComment Create(Guid userId, Guid videoId, string comment, Guid? parentCommentId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException("Comment cannot be empty", nameof(comment));

        var videoComment = new VideoComment
        {
            UserId = userId,
            VideoId = videoId,
            ParentCommentId = parentCommentId,
            Comment = comment,
            IsEdited = false,
            UpdatedAt = DateTime.UtcNow
        };

        return videoComment;
    }

    /// <summary>
    /// Updates comment text
    /// </summary>
    public void Update(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException("Comment cannot be empty", nameof(comment));

        Comment = comment;
        IsEdited = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
