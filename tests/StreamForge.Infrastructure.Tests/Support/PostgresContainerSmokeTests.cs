using FluentAssertions;
using Testcontainers.PostgreSql;

namespace StreamForge.Infrastructure.Tests.Support;

public sealed class PostgresContainerSmokeTests
{
    [Fact(Skip = "Docker-backed integration smoke test. Enable when running the full integration suite locally or in CI.")]
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
