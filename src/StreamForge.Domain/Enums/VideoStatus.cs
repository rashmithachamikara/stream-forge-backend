namespace StreamForge.Domain.Enums;

/// <summary>
/// Lifecycle status for a video.
/// </summary>
public enum VideoStatus
{
    /// <summary>
    /// Upload session has started but no playable file exists yet.
    /// </summary>
    Uploading = 1,

    /// <summary>
    /// File exists and downstream processing is running.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Video is ready to be listed and played.
    /// </summary>
    Ready = 3,

    /// <summary>
    /// Upload or processing failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Video has been deleted or tombstoned.
    /// </summary>
    Deleted = 5
}
