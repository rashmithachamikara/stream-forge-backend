namespace StreamForge.Application.Common;

/// <summary>
/// Configures the administrator account created by baseline database seeding.
/// </summary>
public sealed class SeedAdminOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SeedAdmin";

    public string Name { get; set; } = "Administrator";

    public string Email { get; set; } = "admin@streamforge.local";

    /// <summary>
    /// Gets or sets the initial password. When empty, no administrator is created.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
