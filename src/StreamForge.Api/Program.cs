using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StreamForge.Api.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using StreamForge.Api.Authentication;
using StreamForge.Api.Middleware;
using RateLimiterConfigOptions = StreamForge.Application.Common.RateLimiterOptions;
using StreamForge.Application.Common;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Content;
using StreamForge.Application.UseCases.Engagement;
using StreamForge.Application.UseCases.Uploads;
using StreamForge.Application.UseCases.Uploads.CreateSession;
using StreamForge.Application.UseCases.Processing;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Authentication;
using StreamForge.Infrastructure.Data;
using StreamForge.Infrastructure.Persistence;
using StreamForge.Infrastructure.Processing;
using StreamForge.Infrastructure.Storage;

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
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .Configure<VideoProcessingOptions>(builder.Configuration.GetSection(VideoProcessingOptions.SectionName))
    .AddOptions<VideoProcessingOptions>()
    .ValidateDataAnnotations()
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
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IAuthorizationService, VideoAuthorizationService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<UploadOptions>>().Value);
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<VideoProcessingOptions>>().Value);
builder.Services.AddScoped<IStorageService>(sp =>
{
    var uploadOptions = sp.GetRequiredService<IOptions<UploadOptions>>().Value;
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<LocalFileStorageService>>();
    var storagePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, uploadOptions.StoragePath));
    return new LocalFileStorageService(storagePath, logger);
});
builder.Services.AddScoped<IMediaProcessingService>(sp =>
{
    var uploadOptions = sp.GetRequiredService<IOptions<UploadOptions>>().Value;
    var processingOptions = sp.GetRequiredService<IOptions<VideoProcessingOptions>>().Value;
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<LocalFfmpegMediaProcessingService>>();
    var storagePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, uploadOptions.StoragePath));
    return new LocalFfmpegMediaProcessingService(storagePath, processingOptions, logger);
});
builder.Services.AddScoped<IVideoProcessingQueue, HangfireVideoProcessingQueue>();
builder.Services.AddScoped<CreateUploadSessionService>();
builder.Services.AddScoped<GetUploadTargetService>();
builder.Services.AddScoped<UploadPartService>();
builder.Services.AddScoped<CompleteUploadSessionService>();
builder.Services.AddScoped<ProcessVideoJobService>();
builder.Services.AddScoped<GetPlaybackManifestService>();
builder.Services.AddScoped<GetStreamingAssetService>();
builder.Services.AddScoped<GetVideoThumbnailService>();
builder.Services.AddScoped<ListVideosService>();
builder.Services.AddScoped<GetVideoDetailsService>();
builder.Services.AddScoped<ListMyVideosService>();
builder.Services.AddScoped<UpdateVideoService>();
builder.Services.AddScoped<ArchiveVideoService>();
builder.Services.AddScoped<GetVideoProcessingStatusService>();
builder.Services.AddScoped<ListCategoriesService>();
builder.Services.AddScoped<GetCategoryService>();
builder.Services.AddScoped<ListTagsService>();
builder.Services.AddScoped<GetTagService>();
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

// Configure database
builder.Services.AddDbContext<StreamForgeDbContext>(options =>
    options.UseNpgsql(connectionStrings.DefaultConnection));

var app = builder.Build();

// Check database migration status and seed data when schema is current.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StreamForgeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");
    var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToArray();

    if (pendingMigrations.Length > 0)
    {
        logger.LogWarning(
            "Database has {MigrationCount} pending migration(s): {PendingMigrations}. Run database migrations before starting the application in this environment.",
            pendingMigrations.Length,
            string.Join(", ", pendingMigrations));
    }
    else
    {
        var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataSeeder).FullName!);
        await DataSeeder.SeedAsync(context, seedLogger);
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

app.UseHttpsRedirection();

app.MapControllers();

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
