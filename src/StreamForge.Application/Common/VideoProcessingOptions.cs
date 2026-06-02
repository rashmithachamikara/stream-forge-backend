using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

public sealed class VideoProcessingOptions
{
    public const string SectionName = "VideoProcessing";

    [Required]
    public string FfmpegPath { get; set; } = "ffmpeg";

    [Required]
    public string FfprobePath { get; set; } = "ffprobe";

    [Range(1, 30)]
    public int HlsSegmentSeconds { get; set; } = 6;

    [Range(1, 100)]
    public int ThumbnailTimestampPercent { get; set; } = 10;
}
