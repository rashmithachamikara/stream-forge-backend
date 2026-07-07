namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a single part/chunk of an upload session
/// </summary>
public class UploadSessionPart : BaseEntity
{
    /// <summary>
    /// Session this part belongs to
    /// </summary>
    public Guid UploadSessionId { get; private set; }

    /// <summary>
    /// Part number (1-based index)
    /// </summary>
    public int PartNumber { get; private set; }

    /// <summary>
    /// Size of this part (in bytes)
    /// </summary>
    public long Size { get; private set; }

    /// <summary>
    /// MD5 or SHA256 checksum of the part
    /// </summary>
    public string Checksum { get; private set; } = string.Empty;

    /// <summary>
    /// Storage location for this part (path or S3 key)
    /// </summary>
    public string StoragePath { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this part has been fully uploaded
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// When the part was uploaded
    /// </summary>
    public DateTime UploadedAt { get; private set; }

    private UploadSessionPart() { }

    /// <summary>
    /// Creates a new upload session part
    /// </summary>
    public static UploadSessionPart Create(
        Guid uploadSessionId,
        int partNumber,
        long size,
        string checksum,
        string storagePath)
    {
        return new UploadSessionPart
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UploadSessionId = uploadSessionId,
            PartNumber = partNumber,
            Size = size,
            Checksum = checksum,
            StoragePath = storagePath,
            IsComplete = false,
            UploadedAt = DateTime.MinValue
        };
    }

    /// <summary>
    /// Marks this part as complete
    /// </summary>
    public void MarkAsComplete()
    {
        IsComplete = true;
        UploadedAt = DateTime.UtcNow;
    }
}
