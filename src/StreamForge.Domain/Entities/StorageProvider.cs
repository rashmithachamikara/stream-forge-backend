using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a storage provider configuration
/// </summary>
public class StorageProvider : BaseEntity
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Provider type
    /// </summary>
    public StorageProviderType Type { get; private set; }

    /// <summary>
    /// Provider-specific configuration (JSON)
    /// </summary>
    public string Configuration { get; private set; }

    /// <summary>
    /// Whether this is the default storage provider
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Provider active status
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<VideoFile> VideoFiles { get; private set; }

    // Private constructor for EF Core
    private StorageProvider() : base()
    {
        Name = string.Empty;
        Configuration = string.Empty;
        VideoFiles = new List<VideoFile>();
    }

    /// <summary>
    /// Creates a new storage provider
    /// </summary>
    public static StorageProvider Create(string name, StorageProviderType type, string configuration, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Provider name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(configuration))
            throw new ArgumentException("Configuration cannot be empty", nameof(configuration));

        var provider = new StorageProvider
        {
            Name = name,
            Type = type,
            Configuration = configuration,
            IsDefault = isDefault,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        return provider;
    }

    /// <summary>
    /// Updates provider configuration
    /// </summary>
    public void UpdateConfiguration(string name, string configuration)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Provider name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(configuration))
            throw new ArgumentException("Configuration cannot be empty", nameof(configuration));

        Name = name;
        Configuration = configuration;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets as default provider
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes default status
    /// </summary>
    public void RemoveDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the provider
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the provider
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
