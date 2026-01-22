namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a video thumbnail image
/// </summary>
public class VideoThumbnail : BaseEntity
{
    /// <summary>
    /// Video ID
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// Storage path for thumbnail
    /// </summary>
    public string StoragePath { get; private set; }

    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Whether this is the default thumbnail
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Video timestamp in seconds where thumbnail was captured
    /// </summary>
    public int? TimestampSeconds { get; private set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long SizeBytes { get; private set; }

    // Navigation properties
    public Video Video { get; private set; } = null!;

    // Private constructor for EF Core
    private VideoThumbnail() : base()
    {
        StoragePath = string.Empty;
    }

    /// <summary>
    /// Creates a new video thumbnail
    /// </summary>
    public static VideoThumbnail Create(
        Guid videoId,
        string storagePath,
        int width,
        int height,
        long sizeBytes,
        bool isDefault = false,
        int? timestampSeconds = null)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty", nameof(storagePath));

        if (width <= 0)
            throw new ArgumentException("Width must be greater than zero", nameof(width));

        if (height <= 0)
            throw new ArgumentException("Height must be greater than zero", nameof(height));

        if (sizeBytes <= 0)
            throw new ArgumentException("Size must be greater than zero", nameof(sizeBytes));

        var thumbnail = new VideoThumbnail
        {
            VideoId = videoId,
            StoragePath = storagePath,
            Width = width,
            Height = height,
            IsDefault = isDefault,
            TimestampSeconds = timestampSeconds,
            SizeBytes = sizeBytes
        };

        return thumbnail;
    }

    /// <summary>
    /// Sets as default thumbnail
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
    }

    /// <summary>
    /// Removes default status
    /// </summary>
    public void RemoveDefault()
    {
        IsDefault = false;
    }
}
