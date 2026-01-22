using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a specific version of a video (resolution, format, etc.)
/// </summary>
public class VideoVersion : BaseEntity
{
    /// <summary>
    /// Parent video ID
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// Video resolution (e.g., "1080p", "720p", "480p")
    /// </summary>
    public string Resolution { get; private set; }

    /// <summary>
    /// Video format
    /// </summary>
    public VideoFormat Format { get; private set; }

    /// <summary>
    /// Bitrate in kbps
    /// </summary>
    public int? Bitrate { get; private set; }

    /// <summary>
    /// Video codec (e.g., "h264", "h265", "vp9")
    /// </summary>
    public string? Codec { get; private set; }

    /// <summary>
    /// Storage path (relative to provider)
    /// </summary>
    public string StoragePath { get; private set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long SizeBytes { get; private set; }

    /// <summary>
    /// Video duration in seconds
    /// </summary>
    public int DurationSeconds { get; private set; }

    // Navigation properties
    public Video Video { get; private set; } = null!;
    public ICollection<VideoFile> Files { get; private set; }

    // Private constructor for EF Core
    private VideoVersion() : base()
    {
        Resolution = string.Empty;
        StoragePath = string.Empty;
        Files = new List<VideoFile>();
    }

    /// <summary>
    /// Creates a new video version
    /// </summary>
    public static VideoVersion Create(
        Guid videoId,
        string resolution,
        VideoFormat format,
        string storagePath,
        long sizeBytes,
        int durationSeconds,
        int? bitrate = null,
        string? codec = null)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (string.IsNullOrWhiteSpace(resolution))
            throw new ArgumentException("Resolution cannot be empty", nameof(resolution));

        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty", nameof(storagePath));

        if (sizeBytes <= 0)
            throw new ArgumentException("Size must be greater than zero", nameof(sizeBytes));

        if (durationSeconds <= 0)
            throw new ArgumentException("Duration must be greater than zero", nameof(durationSeconds));

        var version = new VideoVersion
        {
            VideoId = videoId,
            Resolution = resolution,
            Format = format,
            Bitrate = bitrate,
            Codec = codec,
            StoragePath = storagePath,
            SizeBytes = sizeBytes,
            DurationSeconds = durationSeconds
        };

        return version;
    }

    /// <summary>
    /// Updates version metadata
    /// </summary>
    public void UpdateMetadata(string storagePath, long sizeBytes, int durationSeconds)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty", nameof(storagePath));

        if (sizeBytes <= 0)
            throw new ArgumentException("Size must be greater than zero", nameof(sizeBytes));

        if (durationSeconds <= 0)
            throw new ArgumentException("Duration must be greater than zero", nameof(durationSeconds));

        StoragePath = storagePath;
        SizeBytes = sizeBytes;
        DurationSeconds = durationSeconds;
    }
}
