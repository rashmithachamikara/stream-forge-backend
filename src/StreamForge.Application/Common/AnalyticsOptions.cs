using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

/// <summary>
/// Configures playback analytics ingestion and reporting behavior.
/// </summary>
public sealed class AnalyticsOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Analytics";

    /// <summary>
    /// Gets or sets whether analytics features are enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether analytics event ingestion is enabled.
    /// </summary>
    public bool IngestionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether owner and user-facing reporting endpoints are enabled.
    /// </summary>
    public bool ReportingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether admin reporting endpoints are enabled.
    /// </summary>
    public bool AdminReportingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether raw analytics events should be persisted.
    /// </summary>
    public bool CollectRawEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether anonymous viewer events are accepted.
    /// </summary>
    public bool CollectAnonymousEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether user-agent values are stored for analytics.
    /// </summary>
    public bool CollectUserAgent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether client IP addresses are stored for analytics.
    /// </summary>
    public bool CollectIpAddress { get; set; } = true;

    /// <summary>
    /// Gets or sets whether pause events are accepted.
    /// </summary>
    public bool CollectPauseEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether seek events are accepted.
    /// </summary>
    public bool CollectSeekEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether player close events are accepted.
    /// </summary>
    public bool CollectCloseEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether device breakdown reporting is enabled.
    /// </summary>
    public bool EnableDeviceBreakdown { get; set; } = true;

    /// <summary>
    /// Gets or sets whether browser breakdown reporting is enabled.
    /// </summary>
    public bool EnableBrowserBreakdown { get; set; } = true;

    /// <summary>
    /// Gets or sets whether active-viewer metrics are enabled.
    /// </summary>
    public bool EnableActiveViewerMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether peak watch-time metrics are enabled.
    /// </summary>
    public bool EnablePeakWatchTimeMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum watch time, in seconds, required to count a view.
    /// </summary>
    [Range(1, 3600)]
    public int MinimumViewWatchSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the active-viewer lookback window in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int ActiveViewerWindowMinutes { get; set; } = 5;
}
