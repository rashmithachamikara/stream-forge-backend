using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents an active upload session for a video
/// </summary>
public class UploadSession : BaseEntity
{
    /// <summary>
    /// User uploading the video
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// User navigation property
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Current status of the upload session
    /// </summary>
    public UploadSessionStatus Status { get; private set; }

    /// <summary>
    /// Total size of the video being uploaded (in bytes)
    /// </summary>
    public long TotalSize { get; private set; }

    /// <summary>
    /// Current size uploaded (in bytes)
    /// </summary>
    public long UploadedSize { get; private set; }

    /// <summary>
    /// Type of storage provider for this upload
    /// </summary>
    public StorageProviderType StorageProviderType { get; private set; }

    /// <summary>
    /// Temporary storage path (for local filesystem) or S3 upload ID
    /// </summary>
    public string TemporaryStoragePath { get; private set; }

    /// <summary>
    /// Video title
    /// </summary>
    public string VideoTitle { get; private set; }

    /// <summary>
    /// Video description
    /// </summary>
    public string? VideoDescription { get; private set; }

    /// <summary>
    /// Video visibility setting
    /// </summary>
    public VideoVisibility VideoVisibility { get; private set; }

    /// <summary>
    /// Category ID (optional)
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// File MIME type
    /// </summary>
    public string? ContentType { get; private set; }

    /// <summary>
    /// Session expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// When the session was last updated
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// The video ID after successful completion
    /// </summary>
    public Guid? VideoId { get; private set; }

    private UploadSession() { }

    /// <summary>
    /// Creates a new upload session
    /// </summary>
    public static UploadSession Create(
        Guid userId,
        string videoTitle,
        long totalSize,
        StorageProviderType storageProviderType,
        string temporaryStoragePath,
        VideoVisibility visibility = VideoVisibility.Private,
        string? videoDescription = null,
        Guid? categoryId = null,
        string? contentType = null,
        int sessionExpirationMinutes = 1440) // Default 24 hours
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(sessionExpirationMinutes);

        return new UploadSession
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            Status = UploadSessionStatus.Created,
            TotalSize = totalSize,
            UploadedSize = 0,
            StorageProviderType = storageProviderType,
            TemporaryStoragePath = temporaryStoragePath,
            VideoTitle = videoTitle,
            VideoDescription = videoDescription,
            VideoVisibility = visibility,
            CategoryId = categoryId,
            ContentType = contentType,
            ExpiresAt = expiresAt,
            UpdatedAt = DateTime.UtcNow,
            VideoId = null
        };
    }

    /// <summary>
    /// Marks session as active
    /// </summary>
    public void MarkAsActive()
    {
        Status = UploadSessionStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates uploaded size
    /// </summary>
    public void UpdateUploadedSize(long uploadedSize)
    {
        UploadedSize = uploadedSize;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks session as completing
    /// </summary>
    public void MarkAsCompleting()
    {
        Status = UploadSessionStatus.Completing;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks session as completed with the video ID
    /// </summary>
    public void MarkAsCompleted(Guid videoId)
    {
        Status = UploadSessionStatus.Completed;
        VideoId = videoId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks session as failed
    /// </summary>
    public void MarkAsFailed()
    {
        Status = UploadSessionStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks session as expired
    /// </summary>
    public void MarkAsExpired()
    {
        Status = UploadSessionStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if session is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Gets progress as percentage
    /// </summary>
    public decimal GetProgress() => TotalSize == 0 ? 0 : (decimal)UploadedSize / TotalSize * 100;
}
