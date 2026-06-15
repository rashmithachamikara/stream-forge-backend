using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StreamForge.Application.Common;

namespace StreamForge.Api.Health;

public sealed class StoragePathHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _environment;
    private readonly UploadOptions _uploadOptions;

    public StoragePathHealthCheck(IWebHostEnvironment environment, IOptions<UploadOptions> uploadOptions)
    {
        _environment = environment;
        _uploadOptions = uploadOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var storagePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _uploadOptions.StoragePath));

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
