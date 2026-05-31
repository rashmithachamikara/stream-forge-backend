using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a video analytics event
/// </summary>
public class AnalyticsEvent : BaseEntity
{
    /// <summary>
    /// Related video ID
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// User ID (optional for anonymous viewing)
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Session identifier for grouping events
    /// </summary>
    public Guid SessionId { get; private set; }

    /// <summary>
    /// Event type
    /// </summary>
    public AnalyticsEventType EventType { get; private set; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime EventTime { get; private set; }

    /// <summary>
    /// Playback position in seconds when event occurred
    /// </summary>
    public int? Position { get; private set; }

    /// <summary>
    /// Duration watched in this event
    /// </summary>
    public int? DurationWatched { get; private set; }

    /// <summary>
    /// User's IP address
    /// </summary>
    public string IpAddress { get; private set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; private set; }

    // Navigation properties
    public Video Video { get; private set; } = null!;
    public User? User { get; private set; }

    // Private constructor for EF Core
    private AnalyticsEvent() : base()
    {
        IpAddress = string.Empty;
    }

    /// <summary>
    /// Creates a new analytics event
    /// </summary>
    public static AnalyticsEvent Create(
        Guid videoId,
        Guid sessionId,
        AnalyticsEventType eventType,
        string ipAddress,
        Guid? userId = null,
        DateTime? eventTime = null,
        int? position = null,
        int? durationWatched = null,
        string? userAgent = null)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session ID is required", nameof(sessionId));

        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address is required", nameof(ipAddress));

        if (position.HasValue && position.Value < 0)
            throw new ArgumentException("Position cannot be negative", nameof(position));

        if (durationWatched.HasValue && durationWatched.Value < 0)
            throw new ArgumentException("Duration watched cannot be negative", nameof(durationWatched));

        var analyticsEvent = new AnalyticsEvent
        {
            VideoId = videoId,
            UserId = userId,
            SessionId = sessionId,
            EventType = eventType,
            EventTime = eventTime ?? DateTime.UtcNow,
            Position = position,
            DurationWatched = durationWatched,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        return analyticsEvent;
    }
}
