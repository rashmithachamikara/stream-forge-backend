using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using StreamForge.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace StreamForge.Infrastructure.Tests.Support;

public sealed class PostgresIntegrationFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("streamforge_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public StreamForgeDbContext CreateContext()
    {
        if (_container is null)
        {
            throw new InvalidOperationException(
                "PostgreSQL integration tests are disabled. Set STREAMFORGE_RUN_POSTGRES_TESTS=true to run them.");
        }

        var options = new DbContextOptionsBuilder<StreamForgeDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsqlOptions => npgsqlOptions.UseVector())
            .Options;

        return new StreamForgeDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("STREAMFORGE_RUN_POSTGRES_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
