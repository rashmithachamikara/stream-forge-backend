namespace StreamForge.Domain.Exceptions;

/// <summary>
/// Exception thrown when a duplicate entity is detected
/// </summary>
public class DuplicateEntityException : DomainException
{
    public string EntityName { get; }
    public string PropertyName { get; }
    public object PropertyValue { get; }

    public DuplicateEntityException(string entityName, string propertyName, object propertyValue)
        : base($"{entityName} with {propertyName} '{propertyValue}' already exists.")
    {
        EntityName = entityName;
        PropertyName = propertyName;
        PropertyValue = propertyValue;
    }
}
