using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

/// <summary>
/// Upload configuration options
/// </summary>
public class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>
    /// Root directory for storing uploaded files (local filesystem)
    /// </summary>
    [Required]
    public string StoragePath { get; set; } = "./uploads";

    /// <summary>
    /// Maximum total file size in bytes (default 5GB)
    /// </summary>
    public long MaxFileSize { get; set; } = 5_368_709_120;

    /// <summary>
    /// Chunk size in bytes (default: 5MB)
    /// </summary>
    public int ChunkSize { get; set; } = 5_242_880;

    /// <summary>
    /// Allowed video MIME types
    /// </summary>
    public string[] AllowedMimeTypes { get; set; } = new[]
    {
        "video/mp4",
        "video/webm",
        "video/quicktime",
        "video/x-msvideo",
        "video/x-matroska",
        "video/mpeg"
    };

    /// <summary>
    /// Session expiration time in minutes (default: 24 hours)
    /// </summary>
    public int SessionExpirationMinutes { get; set; } = 1440;

    /// <summary>
    /// Storage provider type: "local" or "s3"
    /// </summary>
    public string StorageProviderType { get; set; } = "local";
}
