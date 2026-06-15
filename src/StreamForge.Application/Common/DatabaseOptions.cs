namespace StreamForge.Application.Common;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrationsOnStartup { get; set; } = false;

    public bool SeedOnStartup { get; set; } = true;

    public bool WarnOnPendingMigrations { get; set; } = true;
}
