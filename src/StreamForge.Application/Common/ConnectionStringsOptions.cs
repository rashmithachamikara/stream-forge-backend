using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

/// <summary>
/// Configures application connection strings.
/// </summary>
public sealed class ConnectionStringsOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// Gets or sets the primary PostgreSQL connection string used by the API and Hangfire.
    /// </summary>
    [Required(ErrorMessage = "DefaultConnection is required")]
    public string DefaultConnection { get; set; } = string.Empty;
}
