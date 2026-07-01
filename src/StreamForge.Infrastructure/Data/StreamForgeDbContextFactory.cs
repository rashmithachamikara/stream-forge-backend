using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace StreamForge.Infrastructure.Data;

public sealed class StreamForgeDbContextFactory : IDesignTimeDbContextFactory<StreamForgeDbContext>
{
    public StreamForgeDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("STREAMFORGE_MIGRATIONS_CONNECTION_STRING")
            ?? "Host=localhost;Database=streamforge_design;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<StreamForgeDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseVector());

        return new StreamForgeDbContext(optionsBuilder.Options);
    }
}
