namespace StreamForge.Application.Common;

/// <summary>
/// Configures startup-time database migration and seeding behavior.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Gets or sets whether pending migrations should be applied on startup.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; } = false;

    /// <summary>
    /// Gets or sets whether baseline data seeding should run when the schema is current.
    /// </summary>
    public bool SeedOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets whether startup should warn when pending migrations exist.
    /// </summary>
    public bool WarnOnPendingMigrations { get; set; } = true;
}
