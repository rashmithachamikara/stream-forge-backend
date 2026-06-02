using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
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
using StreamForge.Application.UseCases.Uploads;
using StreamForge.Application.UseCases.Uploads.CreateSession;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Authentication;
using StreamForge.Infrastructure.Data;
using StreamForge.Infrastructure.Persistence;
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
builder.Services.AddScoped<IStorageService>(sp =>
{
    var uploadOptions = sp.GetRequiredService<IOptions<UploadOptions>>().Value;
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<LocalFileStorageService>>();
    var storagePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, uploadOptions.StoragePath));
    return new LocalFileStorageService(storagePath, logger);
});
builder.Services.AddScoped<CreateUploadSessionService>();
builder.Services.AddScoped<GetUploadTargetService>();
builder.Services.AddScoped<UploadPartService>();
builder.Services.AddScoped<CompleteUploadSessionService>();

// Configure rate limiting. Upload chunk traffic has a separate bucket because
// large files can legitimately require hundreds of requests in a short burst.
builder.Services.AddRateLimiter(limiterOptions =>
{
    limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var isUploadChunkRequest =
            httpContext.Request.Path.StartsWithSegments("/api/v1/uploads/sessions") &&
            (httpContext.Request.Path.Value?.Contains("/parts/", StringComparison.OrdinalIgnoreCase) == true ||
             httpContext.Request.Path.Value?.EndsWith("/target", StringComparison.OrdinalIgnoreCase) == true);

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"{(isUploadChunkRequest ? "upload" : "api")}:{remoteIp}",
            factory: _ =>
            {
                var permitLimit = isUploadChunkRequest
                    ? rateLimiterOptions.UploadPermitLimit
                    : rateLimiterOptions.PermitLimit;
                var segmentsPerWindow = isUploadChunkRequest
                    ? rateLimiterOptions.UploadSegmentsPerWindow
                    : rateLimiterOptions.SegmentsPerWindow;
                var windowMinutes = isUploadChunkRequest
                    ? rateLimiterOptions.UploadWindowMinutes
                    : rateLimiterOptions.WindowMinutes;

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
