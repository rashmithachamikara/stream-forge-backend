using FluentAssertions;
using Testcontainers.PostgreSql;

namespace StreamForge.Infrastructure.Tests.Support;

public sealed class PostgresContainerSmokeTests
{
    [IntegrationFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSqlContainer_ShouldStart()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("streamforge_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await container.StartAsync();

        container.GetConnectionString().Should().Contain("streamforge_tests");
    }
}
