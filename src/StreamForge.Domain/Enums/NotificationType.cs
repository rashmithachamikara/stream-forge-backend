namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines notification types
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// New comment on user's video
    /// </summary>
    Comment = 1,

    /// <summary>
    /// Video received a like
    /// </summary>
    Like = 2,

    /// <summary>
    /// New video uploaded by followed user
    /// </summary>
    Upload = 3,

    /// <summary>
    /// Video processing completed
    /// </summary>
    ProcessingComplete = 4,

    /// <summary>
    /// Reply to user's comment
    /// </summary>
    Reply = 5
}
