using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    public bool Enabled { get; set; } = true;

    public bool IngestionEnabled { get; set; } = true;

    public bool ReportingEnabled { get; set; } = true;

    public bool AdminReportingEnabled { get; set; } = true;

    public bool CollectRawEvents { get; set; } = true;

    public bool CollectAnonymousEvents { get; set; } = true;

    public bool CollectUserAgent { get; set; } = true;

    public bool CollectIpAddress { get; set; } = true;

    public bool CollectPauseEvents { get; set; } = true;

    public bool CollectSeekEvents { get; set; } = true;

    public bool CollectCloseEvents { get; set; } = true;

    public bool EnableDeviceBreakdown { get; set; } = true;

    public bool EnableBrowserBreakdown { get; set; } = true;

    public bool EnableActiveViewerMetrics { get; set; } = true;

    public bool EnablePeakWatchTimeMetrics { get; set; } = true;

    [Range(1, 3600)]
    public int MinimumViewWatchSeconds { get; set; } = 30;

    [Range(1, 1440)]
    public int ActiveViewerWindowMinutes { get; set; } = 5;
}
