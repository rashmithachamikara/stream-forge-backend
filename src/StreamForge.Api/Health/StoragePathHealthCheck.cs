using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StreamForge.Api.Options;
using StreamForge.Application.Common;

namespace StreamForge.Api.Health;

public sealed class StoragePathHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _environment;
    private readonly StorageOptions _storageOptions;

    public StoragePathHealthCheck(
        IWebHostEnvironment environment,
        IOptions<StorageOptions> storageOptions)
    {
        _environment = environment;
        _storageOptions = storageOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var storagePath = LocalStoragePathResolver.ResolveEffectiveUploadStorageRoot(
            _environment.ContentRootPath,
            _storageOptions.Local);

        try
        {
            Directory.CreateDirectory(storagePath);

            var probeFilePath = Path.Combine(storagePath, $".healthcheck-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(probeFilePath, "ok", cancellationToken);
            File.Delete(probeFilePath);

            return HealthCheckResult.Healthy("Upload storage path is writable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Upload storage path is not writable.", ex);
        }
    }
}
