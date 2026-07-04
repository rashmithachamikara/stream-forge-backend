namespace StreamForge.Domain.Exceptions;

/// <summary>
/// Exception thrown when an upstream external service fails.
/// </summary>
public class ExternalServiceException : DomainException
{
    public string ServiceName { get; }

    public ExternalServiceException(string serviceName, string message)
        : base(message)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}

/// <summary>
/// Exception thrown when an upstream external service rate limits requests.
/// </summary>
public sealed class ExternalServiceThrottledException : ExternalServiceException
{
    public TimeSpan? RetryAfter { get; }

    public ExternalServiceThrottledException(string serviceName, string message, TimeSpan? retryAfter = null)
        : base(serviceName, message)
    {
        RetryAfter = retryAfter;
    }
}
