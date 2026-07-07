namespace StreamForge.Application.Common;

/// <summary>
/// Configures ASP.NET Core Data Protection settings used for encrypted system secrets.
/// </summary>
public sealed class DataProtectionOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DataProtection";

    /// <summary>
    /// Gets or sets the shared Data Protection application name.
    /// </summary>
    public string ApplicationName { get; set; } = "StreamForge";

    /// <summary>
    /// Gets or sets the optional key-ring storage path.
    /// </summary>
    public string? KeysPath { get; set; }
}
