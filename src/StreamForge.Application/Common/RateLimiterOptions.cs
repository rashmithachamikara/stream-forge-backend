using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

public sealed class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";

    [Range(1, 100000, ErrorMessage = "PermitLimit must be between 1 and 100000")]
    public int PermitLimit { get; set; } = 100;

    [Range(1, 1440, ErrorMessage = "WindowMinutes must be between 1 and 1440")]
    public int WindowMinutes { get; set; } = 1;

    [Range(1, 1000, ErrorMessage = "SegmentsPerWindow must be between 1 and 1000")]
    public int SegmentsPerWindow { get; set; } = 8;
}
