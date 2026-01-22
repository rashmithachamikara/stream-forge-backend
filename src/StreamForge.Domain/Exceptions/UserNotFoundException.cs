namespace StreamForge.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user is not found
/// </summary>
public class UserNotFoundException : EntityNotFoundException
{
    public UserNotFoundException(Guid userId)
        : base("User", userId)
    {
    }

    public UserNotFoundException(string identifier, string identifierType)
        : base("User", $"{identifierType}: {identifier}")
    {
    }
}
