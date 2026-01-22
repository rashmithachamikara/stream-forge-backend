using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a user account in the system
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// User's display name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// User's email address (unique)
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Hashed password
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// User role
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Account active status
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<Video> UploadedVideos { get; private set; }
    public ICollection<Playlist> Playlists { get; private set; }
    public ICollection<Bookmark> Bookmarks { get; private set; }
    public ICollection<VideoReaction> Reactions { get; private set; }
    public ICollection<VideoComment> Comments { get; private set; }
    public ICollection<Notification> Notifications { get; private set; }
    public ICollection<AccessControl> AccessControls { get; private set; }
    public ICollection<AnalyticsEvent> AnalyticsEvents { get; private set; }

    // Private constructor for EF Core
    private User() : base()
    {
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        UploadedVideos = new List<Video>();
        Playlists = new List<Playlist>();
        Bookmarks = new List<Bookmark>();
        Reactions = new List<VideoReaction>();
        Comments = new List<VideoComment>();
        Notifications = new List<Notification>();
        AccessControls = new List<AccessControl>();
        AnalyticsEvents = new List<AnalyticsEvent>();
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    public static User Create(string name, string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        var user = new User
        {
            Name = name,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        return user;
    }

    /// <summary>
    /// Updates user information
    /// </summary>
    public void Update(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        Name = name;
        Email = email.ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates password hash
    /// </summary>
    public void UpdatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes user role
    /// </summary>
    public void ChangeRole(UserRole role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the user account
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
