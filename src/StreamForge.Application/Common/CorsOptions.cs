namespace StreamForge.Application.Common;

/// <summary>
/// Configures browser origins allowed to call the API.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Gets or sets the allowed frontend origins for CORS.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
