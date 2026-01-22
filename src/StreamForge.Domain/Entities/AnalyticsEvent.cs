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
    public string SessionId { get; private set; }

    /// <summary>
    /// Event type
    /// </summary>
    public AnalyticsEventType EventType { get; private set; }

    /// <summary>
    /// Playback position in seconds when event occurred
    /// </summary>
    public int? Timestamp { get; private set; }

    /// <summary>
    /// Event-specific metadata (JSON)
    /// </summary>
    public string? Metadata { get; private set; }

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
        SessionId = string.Empty;
        IpAddress = string.Empty;
    }

    /// <summary>
    /// Creates a new analytics event
    /// </summary>
    public static AnalyticsEvent Create(
        Guid videoId,
        string sessionId,
        AnalyticsEventType eventType,
        string ipAddress,
        Guid? userId = null,
        int? timestamp = null,
        string? metadata = null,
        string? userAgent = null)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));

        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address is required", nameof(ipAddress));

        if (timestamp.HasValue && timestamp.Value < 0)
            throw new ArgumentException("Timestamp cannot be negative", nameof(timestamp));

        var analyticsEvent = new AnalyticsEvent
        {
            VideoId = videoId,
            UserId = userId,
            SessionId = sessionId,
            EventType = eventType,
            Timestamp = timestamp,
            Metadata = metadata,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        return analyticsEvent;
    }

    /// <summary>
    /// Updates the event metadata
    /// </summary>
    public void UpdateMetadata(string metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            throw new ArgumentException("Metadata cannot be empty", nameof(metadata));

        Metadata = metadata;
    }
}
