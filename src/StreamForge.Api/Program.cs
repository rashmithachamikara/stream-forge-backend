using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StreamForge.Api.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using StreamForge.Api.Authentication;
using StreamForge.Api.Health;
using StreamForge.Api.Middleware;
using RateLimiterConfigOptions = StreamForge.Application.Common.RateLimiterOptions;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Analytics;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Analytics;
using StreamForge.Application.UseCases.Content;
using StreamForge.Application.UseCases.Engagement;
using StreamForge.Application.UseCases.Uploads;
using StreamForge.Application.UseCases.Uploads.CreateSession;
using StreamForge.Application.UseCases.Processing;
using StreamForge.Application.UseCases.Transcriptions;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Authentication;
using StreamForge.Infrastructure.Data;
using StreamForge.Infrastructure.Persistence;
using StreamForge.Infrastructure.Processing;
using StreamForge.Infrastructure.Storage;
using StreamForge.Infrastructure.Transcription;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "StreamForgeCors";

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StreamForge API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token as: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<StoragePathHealthCheck>("storage", tags: ["ready"]);

builder.Services
    .Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName))
    .AddOptions<JwtOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .Configure<ConnectionStringsOptions>(builder.Configuration.GetSection(ConnectionStringsOptions.SectionName))
    .AddOptions<ConnectionStringsOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .Configure<RateLimiterConfigOptions>(builder.Configuration.GetSection(RateLimiterConfigOptions.SectionName))
    .AddOptions<RateLimiterConfigOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName))
    .AddOptions<UploadOptions>()
    .ValidateOnStart();

builder.Services
    .Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName))
    .AddOptions<StorageOptions>()
    .ValidateOnStart();

builder.Services
    .Configure<VideoProcessingOptions>(builder.Configuration.GetSection(VideoProcessingOptions.SectionName))
    .AddOptions<VideoProcessingOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .Configure<TranscriptionOptions>(builder.Configuration.GetSection(TranscriptionOptions.SectionName))
    .AddOptions<TranscriptionOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .Configure<AnalyticsOptions>(builder.Configuration.GetSection(AnalyticsOptions.SectionName))
    .AddOptions<AnalyticsOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .AddOptions<DatabaseOptions>()
    .ValidateOnStart();

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

builder.Services.AddSingleton<IValidateOptions<RateLimiterConfigOptions>, RateLimiterOptionsValidator>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

var connectionStrings = builder.Configuration.GetSection(ConnectionStringsOptions.SectionName)
    .Get<ConnectionStringsOptions>()
    ?? throw new InvalidOperationException("ConnectionStrings configuration is missing.");

var rateLimiterOptions = builder.Configuration.GetSection(RateLimiterConfigOptions.SectionName)
    .Get<RateLimiterConfigOptions>()
    ?? throw new InvalidOperationException("RateLimiter configuration is missing.");

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>()
    ?? new DatabaseOptions();

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>()
    ?? new CorsOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedUsers", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("EditorsOnly", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Editor.ToString()));
});

builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRequestMetadataAccessor, RequestMetadataAccessor>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IAuthorizationService, VideoAuthorizationService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<UploadOptions>>().Value);
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<StorageOptions>>().Value);
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<VideoProcessingOptions>>().Value);
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<TranscriptionOptions>>().Value);
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<AnalyticsOptions>>().Value);
builder.Services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
builder.Services.AddScoped<IStorageService>(sp =>
{
    var storageOptions = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<LocalFileStorageService>>();
    var storagePath = LocalStoragePathResolver.ResolveEffectiveUploadStorageRoot(
        environment.ContentRootPath,
        storageOptions.Local);
    return new LocalFileStorageService(storagePath, logger);
});
builder.Services.AddScoped<IMediaProcessingService>(sp =>
{
    var storageOptions = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    var processingOptions = sp.GetRequiredService<IOptions<VideoProcessingOptions>>().Value;
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<LocalFfmpegMediaProcessingService>>();
    var storagePath = LocalStoragePathResolver.ResolveEffectiveUploadStorageRoot(
        environment.ContentRootPath,
        storageOptions.Local);
    return new LocalFfmpegMediaProcessingService(storagePath, processingOptions, logger);
});
builder.Services.AddScoped<IVideoProcessingQueue, HangfireVideoProcessingQueue>();
builder.Services.AddScoped<ITranscriptionQueue, HangfireTranscriptionQueue>();
builder.Services.AddHttpClient<ITranscriptionProvider, LocalFasterWhisperTranscriptionProvider>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<TranscriptionOptions>>().Value;
    client.BaseAddress = new Uri(options.WorkerBaseUrl.TrimEnd('/'));
    client.Timeout = TimeSpan.FromMinutes(options.JobTimeoutMinutes);
});
builder.Services.AddScoped<CreateUploadSessionService>();
builder.Services.AddScoped<GetUploadTargetService>();
builder.Services.AddScoped<UploadPartService>();
builder.Services.AddScoped<CompleteUploadSessionService>();
builder.Services.AddScoped<ProcessVideoJobService>();
builder.Services.AddScoped<ResolveTranscriptionSettingsService>();
builder.Services.AddScoped<GetPlaybackManifestService>();
builder.Services.AddScoped<GetStreamingAssetService>();
builder.Services.AddScoped<GetVideoThumbnailService>();
builder.Services.AddScoped<StartVideoTranscriptionService>();
builder.Services.AddScoped<ListVideoTranscriptionsService>();
builder.Services.AddScoped<ListVideoTranscriptionJobsService>();
builder.Services.AddScoped<ListAdminTranscriptionJobsService>();
builder.Services.AddScoped<GetAdminTranscriptionJobService>();
builder.Services.AddScoped<RetryAdminTranscriptionJobService>();
builder.Services.AddScoped<ResyncAdminTranscriptionJobService>();
builder.Services.AddScoped<GetVideoTranscriptionStatusService>();
builder.Services.AddScoped<GetVideoTranscriptionFileService>();
builder.Services.AddScoped<SearchVideoTranscriptService>();
builder.Services.AddScoped<GetVideoTranscriptionChunksService>();
builder.Services.AddScoped<GetAdminTranscriptionSettingsService>();
builder.Services.AddScoped<UpdateAdminTranscriptionSettingsService>();
builder.Services.AddScoped<CompleteVideoTranscriptionCallbackService>();
builder.Services.AddScoped<ListVideosService>();
builder.Services.AddScoped<GetVideoDetailsService>();
builder.Services.AddScoped<ListMyVideosService>();
builder.Services.AddScoped<UpdateVideoService>();
builder.Services.AddScoped<ArchiveVideoService>();
builder.Services.AddScoped<GetVideoProcessingStatusService>();
builder.Services.AddScoped<ListCategoriesService>();
builder.Services.AddScoped<GetCategoryService>();
builder.Services.AddScoped<CreateCategoryService>();
builder.Services.AddScoped<UpdateCategoryService>();
builder.Services.AddScoped<DeleteCategoryService>();
builder.Services.AddScoped<ListTagsService>();
builder.Services.AddScoped<GetTagService>();
builder.Services.AddScoped<CreateTagService>();
builder.Services.AddScoped<UpdateTagService>();
builder.Services.AddScoped<DeleteTagService>();
builder.Services.AddScoped<GetUserProfileService>();
builder.Services.AddScoped<ListUsersService>();
builder.Services.AddScoped<ListMyUploadSessionsService>();
builder.Services.AddScoped<ListVideoAccessGrantsService>();
builder.Services.AddScoped<CreateVideoAccessGrantService>();
builder.Services.AddScoped<RevokeVideoAccessGrantService>();
builder.Services.AddScoped<GetReactionSummaryService>();
builder.Services.AddScoped<SetReactionService>();
builder.Services.AddScoped<RemoveReactionService>();
builder.Services.AddScoped<ListCommentsService>();
builder.Services.AddScoped<CreateCommentService>();
builder.Services.AddScoped<UpdateCommentService>();
builder.Services.AddScoped<DeleteCommentService>();
builder.Services.AddScoped<ListBookmarksService>();
builder.Services.AddScoped<ListVideoBookmarksService>();
builder.Services.AddScoped<CreateBookmarkService>();
builder.Services.AddScoped<UpdateBookmarkService>();
builder.Services.AddScoped<DeleteBookmarkService>();
builder.Services.AddScoped<ListPlaylistsService>();
builder.Services.AddScoped<ListMyPlaylistsService>();
builder.Services.AddScoped<CreatePlaylistService>();
builder.Services.AddScoped<GetPlaylistService>();
builder.Services.AddScoped<UpdatePlaylistService>();
builder.Services.AddScoped<DeletePlaylistService>();
builder.Services.AddScoped<GetPlaylistVideosService>();
builder.Services.AddScoped<AddPlaylistVideoService>();
builder.Services.AddScoped<RemovePlaylistVideoService>();
builder.Services.AddScoped<ReorderPlaylistVideosService>();
builder.Services.AddScoped<ListNotificationsService>();
builder.Services.AddScoped<GetUnreadNotificationCountService>();
builder.Services.AddScoped<MarkNotificationReadStateService>();
builder.Services.AddScoped<MarkAllNotificationsReadService>();
builder.Services.AddScoped<DeleteNotificationService>();
builder.Services.AddScoped<DeleteReadNotificationsService>();
builder.Services.AddScoped<RecordAnalyticsEventService>();
builder.Services.AddScoped<GetVideoAnalyticsSummaryService>();
builder.Services.AddScoped<GetVideoAnalyticsEngagementService>();
builder.Services.AddScoped<GetVideoAnalyticsTimeSeriesService>();
builder.Services.AddScoped<GetMyAnalyticsSummaryService>();
builder.Services.AddScoped<GetRankedVideosAnalyticsService>();
builder.Services.AddScoped<GetViewsOverTimeAnalyticsService>();
builder.Services.AddScoped<GetDeviceBreakdownAnalyticsService>();
builder.Services.AddScoped<GetBrowserBreakdownAnalyticsService>();
builder.Services.AddScoped<GetAuthBreakdownAnalyticsService>();
builder.Services.AddScoped<GetCategoryBreakdownAnalyticsService>();
builder.Services.AddScoped<GetTagBreakdownAnalyticsService>();
builder.Services.AddScoped<GetActiveViewersAnalyticsService>();
builder.Services.AddScoped<GetPeakWatchTimeAnalyticsService>();
builder.Services.AddScoped<ExportAnalyticsReportService>();
builder.Services.AddScoped<GetAdminAnalyticsSummaryService>();

builder.Services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionStrings.DefaultConnection));
});
builder.Services.AddHangfireServer();

// Configure rate limiting. Upload and playback traffic have separate buckets
// because both can legitimately require many requests in a short burst.
builder.Services.AddRateLimiter(limiterOptions =>
{
    limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var trafficClass = ResolveRateLimitTrafficClass(httpContext);

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"{trafficClass}:{remoteIp}",
            factory: _ =>
            {
                var permitLimit = trafficClass switch
                {
                    "upload" => rateLimiterOptions.UploadPermitLimit,
                    "playback" => rateLimiterOptions.PlaybackPermitLimit,
                    _ => rateLimiterOptions.PermitLimit
                };
                var segmentsPerWindow = trafficClass switch
                {
                    "upload" => rateLimiterOptions.UploadSegmentsPerWindow,
                    "playback" => rateLimiterOptions.PlaybackSegmentsPerWindow,
                    _ => rateLimiterOptions.SegmentsPerWindow
                };
                var windowMinutes = trafficClass switch
                {
                    "upload" => rateLimiterOptions.UploadWindowMinutes,
                    "playback" => rateLimiterOptions.PlaybackWindowMinutes,
                    _ => rateLimiterOptions.WindowMinutes
                };

                return new SlidingWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = permitLimit,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    SegmentsPerWindow = segmentsPerWindow,
                    Window = TimeSpan.FromMinutes(windowMinutes)
                };
            });
    });
});

static string ResolveRateLimitTrafficClass(HttpContext httpContext)
{
    var path = httpContext.Request.Path;
    var pathValue = path.Value;

    if (httpContext.Request.Method.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase) &&
        path.StartsWithSegments("/api/v1/videos") &&
        (pathValue?.Contains("/playback/", StringComparison.OrdinalIgnoreCase) == true ||
         pathValue?.EndsWith("/thumbnail", StringComparison.OrdinalIgnoreCase) == true))
    {
        return "playback";
    }

    if (path.StartsWithSegments("/api/v1/uploads/sessions"))
    {
        var isUploadChunkRequest =
            pathValue?.Contains("/parts/", StringComparison.OrdinalIgnoreCase) == true ||
            pathValue?.EndsWith("/target", StringComparison.OrdinalIgnoreCase) == true;
        if (isUploadChunkRequest)
        {
            return "upload";
        }
    }

    return "api";
}

static bool ShouldUseHttpsRedirection(IConfiguration configuration)
{
    var applicationUrls = configuration["ASPNETCORE_URLS"];
    if (!string.IsNullOrWhiteSpace(applicationUrls))
    {
        return applicationUrls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }

    return configuration
        .GetSection("Kestrel:Endpoints")
        .GetChildren()
        .Any(endpoint => endpoint["Url"]?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true);
}

// Configure database
builder.Services.AddDbContext<StreamForgeDbContext>(options =>
    options.UseNpgsql(connectionStrings.DefaultConnection));

var app = builder.Build();

// Check database migration status and seed data when schema is current.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StreamForgeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");
    string[] pendingMigrations = [];

    if (databaseOptions.ApplyMigrationsOnStartup)
    {
        logger.LogInformation("Applying pending database migrations on startup.");
        await context.Database.MigrateAsync();
    }
    else if (databaseOptions.WarnOnPendingMigrations || databaseOptions.SeedOnStartup)
    {
        pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToArray();

        if (databaseOptions.WarnOnPendingMigrations && pendingMigrations.Length > 0)
        {
            logger.LogWarning(
                "Database has {MigrationCount} pending migration(s): {PendingMigrations}. Run database migrations before starting the application in this environment.",
                pendingMigrations.Length,
                string.Join(", ", pendingMigrations));
        }
    }

    var schemaIsCurrent = databaseOptions.ApplyMigrationsOnStartup || pendingMigrations.Length == 0;
    if (databaseOptions.SeedOnStartup && schemaIsCurrent)
    {
        var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataSeeder).FullName!);
        await DataSeeder.SeedAsync(context, seedLogger);
    }
    else if (databaseOptions.SeedOnStartup && !schemaIsCurrent)
    {
        logger.LogInformation("Skipping data seeding because pending migrations exist and startup auto-migration is disabled.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/hangfire");
}

// Exception handling middleware (outermost layer)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(CorsPolicyName);

// Rate limiting middleware
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (ShouldUseHttpsRedirection(builder.Configuration))
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/v1/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program
{
}
