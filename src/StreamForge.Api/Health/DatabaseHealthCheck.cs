using Microsoft.Extensions.Diagnostics.HealthChecks;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Api.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly StreamForgeDbContext _dbContext;

    public DatabaseHealthCheck(StreamForgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
            : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
    }
}
