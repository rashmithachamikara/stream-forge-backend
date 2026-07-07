namespace StreamForge.Application.Common;

/// <summary>
/// Storage configuration shared by upload, processing, and transcription flows.
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Configuration section name used when binding storage settings.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Active storage provider type for upload-session creation.
    /// </summary>
    public string ProviderType { get; set; } = "local";

    /// <summary>
    /// Local filesystem storage conventions.
    /// </summary>
    public LocalStorageOptions Local { get; set; } = new();

    /// <summary>
    /// Placeholder for future S3-specific configuration.
    /// </summary>
    public S3StorageOptions S3 { get; set; } = new();
}

/// <summary>
/// Placeholder for future S3-specific storage configuration values.
/// </summary>
public class S3StorageOptions
{
}
