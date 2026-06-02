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
    /// Video created for this upload session.
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// Video navigation property.
    /// </summary>
    public Video Video { get; private set; } = null!;

    private UploadSession()
    {
        TemporaryStoragePath = string.Empty;
    }

    /// <summary>
    /// Creates a new upload session
    /// </summary>
    public static UploadSession Create(
        Guid userId,
        Guid videoId,
        long totalSize,
        StorageProviderType storageProviderType,
        string temporaryStoragePath,
        string? contentType = null,
        int sessionExpirationMinutes = 1440) // Default 24 hours
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        var expiresAt = DateTime.UtcNow.AddMinutes(sessionExpirationMinutes);

        return new UploadSession
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            VideoId = videoId,
            Status = UploadSessionStatus.Created,
            TotalSize = totalSize,
            UploadedSize = 0,
            StorageProviderType = storageProviderType,
            TemporaryStoragePath = temporaryStoragePath,
            ContentType = contentType,
            ExpiresAt = expiresAt,
            UpdatedAt = DateTime.UtcNow
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
    /// Marks session as completed
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = UploadSessionStatus.Completed;
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
