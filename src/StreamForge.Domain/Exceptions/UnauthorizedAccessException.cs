namespace StreamForge.Domain.Exceptions;

/// <summary>
/// Exception thrown when unauthorized access is attempted
/// </summary>
public class UnauthorizedAccessException : DomainException
{
    public UnauthorizedAccessException(string message)
        : base(message)
    {
    }

    public UnauthorizedAccessException(string resourceName, Guid resourceId)
        : base($"Unauthorized access to {resourceName} with ID '{resourceId}'.")
    {
    }
}
