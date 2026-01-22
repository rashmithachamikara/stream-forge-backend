namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines analytics event types
/// </summary>
public enum AnalyticsEventType
{
    /// <summary>
    /// Video started playing
    /// </summary>
    Play = 1,

    /// <summary>
    /// Video was paused
    /// </summary>
    Pause = 2,

    /// <summary>
    /// User seeked to a different position
    /// </summary>
    Seek = 3,

    /// <summary>
    /// Video watched to completion
    /// </summary>
    Complete = 4,

    /// <summary>
    /// Video player was closed
    /// </summary>
    Close = 5
}
