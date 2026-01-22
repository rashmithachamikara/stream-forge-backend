using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a user notification
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// Recipient user ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Related video ID (optional)
    /// </summary>
    public Guid? VideoId { get; private set; }

    /// <summary>
    /// Notification type
    /// </summary>
    public NotificationType NotificationType { get; private set; }

    /// <summary>
    /// Notification message
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// Read status
    /// </summary>
    public bool IsRead { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public Video? Video { get; private set; }

    // Private constructor for EF Core
    private Notification() : base()
    {
        Message = string.Empty;
    }

    /// <summary>
    /// Creates a new notification
    /// </summary>
    public static Notification Create(
        Guid userId,
        NotificationType notificationType,
        string message,
        Guid? videoId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        var notification = new Notification
        {
            UserId = userId,
            VideoId = videoId,
            NotificationType = notificationType,
            Message = message,
            IsRead = false
        };

        return notification;
    }

    /// <summary>
    /// Marks notification as read
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
    }

    /// <summary>
    /// Marks notification as unread
    /// </summary>
    public void MarkAsUnread()
    {
        IsRead = false;
    }
}
