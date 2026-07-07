using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

/// <summary>
/// Configures local media-processing behavior such as FFmpeg paths and generated asset settings.
/// </summary>
public sealed class VideoProcessingOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "VideoProcessing";

    /// <summary>
    /// Gets or sets the FFmpeg executable path or command name.
    /// </summary>
    [Required]
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>
    /// Gets or sets the ffprobe executable path or command name.
    /// </summary>
    [Required]
    public string FfprobePath { get; set; } = "ffprobe";

    /// <summary>
    /// Gets or sets the target HLS segment duration in seconds.
    /// </summary>
    [Range(1, 30)]
    public int HlsSegmentSeconds { get; set; } = 6;

    /// <summary>
    /// Gets or sets the thumbnail capture position as a percentage into the video.
    /// </summary>
    [Range(1, 100)]
    public int ThumbnailTimestampPercent { get; set; } = 10;
}
