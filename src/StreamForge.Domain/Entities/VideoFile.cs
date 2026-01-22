namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a physical video file in storage
/// </summary>
public class VideoFile : BaseEntity
{
    /// <summary>
    /// Video version ID
    /// </summary>
    public Guid VideoVersionId { get; private set; }

    /// <summary>
    /// Storage provider ID
    /// </summary>
    public Guid StorageProviderId { get; private set; }

    /// <summary>
    /// Full file path in storage
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// File checksum (SHA-256)
    /// </summary>
    public string? Checksum { get; private set; }

    /// <summary>
    /// MIME type
    /// </summary>
    public string MimeType { get; private set; }

    // Navigation properties
    public VideoVersion VideoVersion { get; private set; } = null!;
    public StorageProvider StorageProvider { get; private set; } = null!;

    // Private constructor for EF Core
    private VideoFile() : base()
    {
        FilePath = string.Empty;
        MimeType = string.Empty;
    }

    /// <summary>
    /// Creates a new video file reference
    /// </summary>
    public static VideoFile Create(
        Guid videoVersionId,
        Guid storageProviderId,
        string filePath,
        long fileSize,
        string mimeType,
        string? checksum = null)
    {
        if (videoVersionId == Guid.Empty)
            throw new ArgumentException("Video version ID is required", nameof(videoVersionId));

        if (storageProviderId == Guid.Empty)
            throw new ArgumentException("Storage provider ID is required", nameof(storageProviderId));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (fileSize <= 0)
            throw new ArgumentException("File size must be greater than zero", nameof(fileSize));

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME type cannot be empty", nameof(mimeType));

        var videoFile = new VideoFile
        {
            VideoVersionId = videoVersionId,
            StorageProviderId = storageProviderId,
            FilePath = filePath,
            FileSize = fileSize,
            Checksum = checksum,
            MimeType = mimeType
        };

        return videoFile;
    }

    /// <summary>
    /// Updates checksum
    /// </summary>
    public void UpdateChecksum(string checksum)
    {
        if (string.IsNullOrWhiteSpace(checksum))
            throw new ArgumentException("Checksum cannot be empty", nameof(checksum));

        Checksum = checksum;
    }
}
