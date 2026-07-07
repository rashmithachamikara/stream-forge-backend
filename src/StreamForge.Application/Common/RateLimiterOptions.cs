using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

/// <summary>
/// Configures API, upload, and playback rate-limiting buckets.
/// </summary>
public sealed class RateLimiterOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "RateLimiter";

    /// <summary>
    /// Gets or sets the general API request limit per window.
    /// </summary>
    [Range(1, 100000, ErrorMessage = "PermitLimit must be between 1 and 100000")]
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the general API limiter window length in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "WindowMinutes must be between 1 and 1440")]
    public int WindowMinutes { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of sliding-window segments for general API traffic.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "SegmentsPerWindow must be between 1 and 1000")]
    public int SegmentsPerWindow { get; set; } = 8;

    /// <summary>
    /// Gets or sets the upload request limit per window.
    /// </summary>
    [Range(1, 100000, ErrorMessage = "UploadPermitLimit must be between 1 and 100000")]
    public int UploadPermitLimit { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the upload limiter window length in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "UploadWindowMinutes must be between 1 and 1440")]
    public int UploadWindowMinutes { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of sliding-window segments for upload traffic.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "UploadSegmentsPerWindow must be between 1 and 1000")]
    public int UploadSegmentsPerWindow { get; set; } = 8;

    /// <summary>
    /// Gets or sets the playback request limit per window.
    /// </summary>
    [Range(1, 100000, ErrorMessage = "PlaybackPermitLimit must be between 1 and 100000")]
    public int PlaybackPermitLimit { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the playback limiter window length in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "PlaybackWindowMinutes must be between 1 and 1440")]
    public int PlaybackWindowMinutes { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of sliding-window segments for playback traffic.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "PlaybackSegmentsPerWindow must be between 1 and 1000")]
    public int PlaybackSegmentsPerWindow { get; set; } = 8;
}
