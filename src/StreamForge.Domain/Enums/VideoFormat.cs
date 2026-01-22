namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines video format types
/// </summary>
public enum VideoFormat
{
    /// <summary>
    /// MP4 container format
    /// </summary>
    Mp4 = 1,

    /// <summary>
    /// WebM container format
    /// </summary>
    WebM = 2,

    /// <summary>
    /// HTTP Live Streaming playlist
    /// </summary>
    HLS = 3,

    /// <summary>
    /// MPEG-DASH adaptive streaming
    /// </summary>
    DASH = 4
}
