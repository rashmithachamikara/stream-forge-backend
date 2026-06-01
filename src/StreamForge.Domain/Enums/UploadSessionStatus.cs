namespace StreamForge.Domain.Enums;

/// <summary>
/// Status of an upload session
/// </summary>
public enum UploadSessionStatus
{
    /// <summary>
    /// Session created, awaiting first part upload
    /// </summary>
    Created = 0,

    /// <summary>
    /// Chunks are being uploaded
    /// </summary>
    Active = 1,

    /// <summary>
    /// All chunks received and assembly in progress
    /// </summary>
    Completing = 2,

    /// <summary>
    /// Upload completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Upload failed or was explicitly cancelled
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Session expired without completion
    /// </summary>
    Expired = 5
}
