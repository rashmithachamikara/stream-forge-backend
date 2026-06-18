using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StreamForge.Infrastructure.Data;
using StreamForge.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace StreamForge.Api.IntegrationTests.Support;

public sealed class ApiIntegrationFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private WebApplicationFactory<Program>? _factory;
    private string? _storagePath;
    private Dictionary<string, string?>? _originalEnvironmentValues;

    public HttpClient Client { get; private set; } = null!;

    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("STREAMFORGE_RUN_API_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        _storagePath = Path.Combine(Path.GetTempPath(), "streamforge-api-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storagePath);

        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("streamforge_api_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();
        await MigrateAndSeedAsync();

        ApplyEnvironmentConfiguration();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(CreateConfiguration());
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                });
            });

        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        RestoreEnvironmentConfiguration();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        if (_storagePath is not null && Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    public async Task ResetDatabaseAsync()
    {
        EnsureEnabled();
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        await DataSeeder.SeedAsync(context);
    }

    public StreamForgeDbContext CreateContext()
    {
        EnsureEnabled();
        var options = new DbContextOptionsBuilder<StreamForgeDbContext>()
            .UseNpgsql(_container!.GetConnectionString())
            .Options;

        return new StreamForgeDbContext(options);
    }

    private async Task MigrateAndSeedAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await DataSeeder.SeedAsync(context);
    }

    private IReadOnlyDictionary<string, string?> CreateConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _container!.GetConnectionString(),
            ["Jwt:Issuer"] = "StreamForge.Tests",
            ["Jwt:Audience"] = "StreamForge.Api.Tests",
            ["Jwt:SigningKey"] = "streamforge-api-integration-tests-signing-key-32-plus-chars",
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:RefreshTokenDays"] = "30",
            ["RateLimiter:PermitLimit"] = "10000",
            ["RateLimiter:WindowMinutes"] = "1",
            ["RateLimiter:SegmentsPerWindow"] = "8",
            ["RateLimiter:UploadPermitLimit"] = "10000",
            ["RateLimiter:UploadWindowMinutes"] = "1",
            ["RateLimiter:UploadSegmentsPerWindow"] = "8",
            ["RateLimiter:PlaybackPermitLimit"] = "10000",
            ["RateLimiter:PlaybackWindowMinutes"] = "1",
            ["RateLimiter:PlaybackSegmentsPerWindow"] = "8",
            ["Upload:MaxFileSize"] = "10485760",
            ["Upload:ChunkSize"] = "1048576",
            ["Upload:AllowedMimeTypes:0"] = "video/mp4",
            ["Upload:AllowedMimeTypes:1"] = "video/webm",
            ["Upload:SessionExpirationMinutes"] = "60",
            ["Storage:ProviderType"] = "local",
            ["Storage:Local:RootPath"] = _storagePath!,
            ["Storage:Local:UploadsRelativePath"] = "uploads",
            ["Storage:Local:TranscriptionOutputRelativePath"] = "transcription-output",
            ["VideoProcessing:FfmpegPath"] = "ffmpeg",
            ["VideoProcessing:FfprobePath"] = "ffprobe",
            ["VideoProcessing:HlsSegmentSeconds"] = "6",
            ["VideoProcessing:ThumbnailTimestampPercent"] = "10",
            ["Analytics:Enabled"] = "true",
            ["Analytics:IngestionEnabled"] = "true",
            ["Analytics:ReportingEnabled"] = "true",
            ["Analytics:AdminReportingEnabled"] = "true",
            ["Analytics:CollectRawEvents"] = "true",
            ["Analytics:CollectAnonymousEvents"] = "true",
            ["Analytics:CollectUserAgent"] = "true",
            ["Analytics:CollectIpAddress"] = "true",
            ["Analytics:CollectPauseEvents"] = "true",
            ["Analytics:CollectSeekEvents"] = "true",
            ["Analytics:CollectCloseEvents"] = "true",
            ["Analytics:EnableDeviceBreakdown"] = "true",
            ["Analytics:EnableBrowserBreakdown"] = "true",
            ["Analytics:EnableActiveViewerMetrics"] = "true",
            ["Analytics:EnablePeakWatchTimeMetrics"] = "true",
            ["Analytics:MinimumViewWatchSeconds"] = "5",
            ["Analytics:ActiveViewerWindowMinutes"] = "5"
        };
    }

    private void ApplyEnvironmentConfiguration()
    {
        var configuration = CreateConfiguration();
        _originalEnvironmentValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in configuration)
        {
            var key = pair.Key.Replace(":", "__", StringComparison.Ordinal);
            _originalEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, pair.Value);
        }
    }

    private void RestoreEnvironmentConfiguration()
    {
        if (_originalEnvironmentValues is null)
        {
            return;
        }

        foreach (var pair in _originalEnvironmentValues)
        {
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }

        _originalEnvironmentValues = null;
    }

    private static void EnsureEnabled()
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("API integration tests are disabled.");
        }
    }
}
