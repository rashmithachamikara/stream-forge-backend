namespace StreamForge.Infrastructure.Tests.Support;

public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (!PostgresIntegrationFixture.IsEnabled)
        {
            Skip = "PostgreSQL integration test. Run with STREAMFORGE_RUN_POSTGRES_TESTS=true and filter Category=PostgresIntegration.";
        }
    }
}
