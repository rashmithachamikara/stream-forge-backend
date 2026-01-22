namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines storage provider types
/// </summary>
public enum StorageProviderType
{
    /// <summary>
    /// Local filesystem storage
    /// </summary>
    Local = 1,

    /// <summary>
    /// AWS S3 or S3-compatible storage
    /// </summary>
    S3 = 2,

    /// <summary>
    /// Microsoft Azure Blob Storage
    /// </summary>
    AzureBlob = 3
}
