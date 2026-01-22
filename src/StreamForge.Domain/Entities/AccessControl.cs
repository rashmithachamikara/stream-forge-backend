using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents video access control and sharing permissions
/// </summary>
public class AccessControl : BaseEntity
{
    /// <summary>
    /// Video ID
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// User ID (null for token-based access)
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Secure sharing token
    /// </summary>
    public string? ShareToken { get; private set; }

    /// <summary>
    /// Permission type
    /// </summary>
    public PermissionType PermissionType { get; private set; }

    /// <summary>
    /// Expiration timestamp (null for permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Active status
    /// </summary>
    public bool IsActive { get; private set; }

    // Navigation properties
    public Video Video { get; private set; } = null!;
    public User? User { get; private set; }

    // Private constructor for EF Core
    private AccessControl() : base()
    {
    }

    /// <summary>
    /// Creates a new access control entry for a user
    /// </summary>
    public static AccessControl CreateForUser(
        Guid videoId,
        Guid userId,
        PermissionType permissionType,
        DateTime? expiresAt = null)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        var accessControl = new AccessControl
        {
            VideoId = videoId,
            UserId = userId,
            PermissionType = permissionType,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        return accessControl;
    }

    /// <summary>
    /// Creates a new access control entry with share token
    /// </summary>
    public static AccessControl CreateWithToken(
        Guid videoId,
        string shareToken,
        PermissionType permissionType,
        DateTime? expiresAt = null)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (string.IsNullOrWhiteSpace(shareToken))
            throw new ArgumentException("Share token cannot be empty", nameof(shareToken));

        var accessControl = new AccessControl
        {
            VideoId = videoId,
            ShareToken = shareToken,
            PermissionType = permissionType,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        return accessControl;
    }

    /// <summary>
    /// Checks if access control is expired
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the access control
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates the access control
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Extends expiration date
    /// </summary>
    public void ExtendExpiration(DateTime newExpiresAt)
    {
        if (newExpiresAt <= DateTime.UtcNow)
            throw new ArgumentException("New expiration must be in the future", nameof(newExpiresAt));

        ExpiresAt = newExpiresAt;
    }
}
