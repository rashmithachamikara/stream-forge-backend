using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a user's reaction (like/dislike) to a video
/// </summary>
public class VideoReaction : BaseEntity
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
    /// Reaction type
    /// </summary>
    public ReactionType ReactionType { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public Video Video { get; private set; } = null!;

    // Private constructor for EF Core
    private VideoReaction() : base()
    {
    }

    /// <summary>
    /// Creates a new video reaction
    /// </summary>
    public static VideoReaction Create(Guid userId, Guid videoId, ReactionType reactionType)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        var reaction = new VideoReaction
        {
            UserId = userId,
            VideoId = videoId,
            ReactionType = reactionType
        };

        return reaction;
    }

    /// <summary>
    /// Changes the reaction type
    /// </summary>
    public void ChangeReaction(ReactionType newReactionType)
    {
        ReactionType = newReactionType;
    }
}
