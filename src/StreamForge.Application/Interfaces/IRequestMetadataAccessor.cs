namespace StreamForge.Application.Interfaces;

/// <summary>
/// Provides request-level metadata used by analytics, auditing, and security flows.
/// </summary>
public interface IRequestMetadataAccessor
{
    /// <summary>
    /// Gets the best-effort client IP address for the current request.
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// Gets the raw user-agent header for the current request when available.
    /// </summary>
    string? UserAgent { get; }
}
