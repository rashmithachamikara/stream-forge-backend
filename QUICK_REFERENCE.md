# Stream Forge - Quick Reference Guide

## Solution Commands

### Build & Run
```bash
# Restore dependencies
dotnet restore

# Build entire solution
dotnet build

# Build specific project
dotnet build src/StreamForge.Api

# Run API project
dotnet run --project src/StreamForge.Api

# Run with hot reload
dotnet watch --project src/StreamForge.Api
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api

# Update database
dotnet ef database update --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api

# Remove last migration
dotnet ef migrations remove --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api

# Generate SQL script
dotnet ef migrations script --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api
```

### Package Management
```bash
# Add package to Domain
dotnet add src/StreamForge.Domain package PackageName

# Add package to Application
dotnet add src/StreamForge.Application package PackageName

# Add package to Infrastructure
dotnet add src/StreamForge.Infrastructure package PackageName

# Add package to API
dotnet add src/StreamForge.Api package PackageName
```

## Project Structure Rules

### Domain Layer ✅
**CAN:**
- Define entities, value objects, enums
- Define domain exceptions
- Define repository interfaces
- Contain pure business logic
- Use System namespaces only

**CANNOT:**
- Reference other projects
- Use EF Core
- Use ASP.NET Core
- Access database
- Make HTTP calls

### Application Layer ✅
**CAN:**
- Define use cases (Commands/Queries)
- Define application interfaces
- Define DTOs
- Reference Domain layer
- Use MediatR, AutoMapper, FluentValidation

**CANNOT:**
- Reference Infrastructure or API
- Implement persistence
- Implement external services
- Know about HTTP or databases

### Infrastructure Layer ✅
**CAN:**
- Implement Application interfaces
- Use EF Core, Dapper
- Access databases
- Call external APIs
- Implement storage services
- Implement background jobs
- Reference Application and Domain

**CANNOT:**
- Contain business logic
- Be referenced by Domain or Application

### API Layer ✅
**CAN:**
- Define controllers
- Handle HTTP requests/responses
- Use middleware
- Configure dependency injection
- Reference all layers

**CANNOT:**
- Contain business logic
- Direct database access
- Be referenced by other layers

## Common Patterns

### Creating a New Entity (Domain)
```csharp
// src/StreamForge.Domain/Entities/Video.cs
namespace StreamForge.Domain.Entities;

public class Video
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private Video() { } // EF Core
    
    public static Video Create(string title)
    {
        // Business logic here
        return new Video
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

### Creating a Repository Interface (Domain)
```csharp
// src/StreamForge.Domain/Interfaces/IVideoRepository.cs
namespace StreamForge.Domain.Interfaces;

public interface IVideoRepository
{
    Task<Video?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Video>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Video video, CancellationToken cancellationToken = default);
    Task UpdateAsync(Video video, CancellationToken cancellationToken = default);
    Task DeleteAsync(Video video, CancellationToken cancellationToken = default);
}
```

### Creating a Use Case (Application)
```csharp
// src/StreamForge.Application/UseCases/Videos/UploadVideo/UploadVideoCommand.cs
using MediatR;

namespace StreamForge.Application.UseCases.Videos.UploadVideo;

public record UploadVideoCommand(string Title, Stream FileStream) : IRequest<VideoDto>;

// UploadVideoCommandHandler.cs
public class UploadVideoCommandHandler : IRequestHandler<UploadVideoCommand, VideoDto>
{
    private readonly IVideoRepository _videoRepository;
    private readonly IStorageService _storageService;
    
    public UploadVideoCommandHandler(
        IVideoRepository videoRepository,
        IStorageService storageService)
    {
        _videoRepository = videoRepository;
        _storageService = storageService;
    }
    
    public async Task<VideoDto> Handle(
        UploadVideoCommand request, 
        CancellationToken cancellationToken)
    {
        // Use case logic here
        var video = Video.Create(request.Title);
        await _videoRepository.AddAsync(video, cancellationToken);
        
        return new VideoDto(video.Id, video.Title);
    }
}
```

### Creating a Controller (API)
```csharp
// src/StreamForge.Api/Controllers/VideosController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public VideosController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] UploadVideoRequest request)
    {
        var command = new UploadVideoCommand(request.Title, request.File.OpenReadStream());
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Implementation
        return Ok();
    }
}
```

### Implementing a Repository (Infrastructure)
```csharp
// src/StreamForge.Infrastructure/Persistence/Repositories/VideoRepository.cs
using Microsoft.EntityFrameworkCore;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly StreamForgeDbContext _context;
    
    public VideoRepository(StreamForgeDbContext context)
    {
        _context = context;
    }
    
    public async Task<Video?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Videos
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(Video video, CancellationToken cancellationToken = default)
    {
        await _context.Videos.AddAsync(video, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

## Naming Conventions

### Files & Folders
- PascalCase for folders: `UseCases`, `Entities`
- PascalCase for files: `VideoRepository.cs`, `UploadVideoCommand.cs`
- One class per file
- File name matches class name

### Code
- PascalCase for: Classes, Methods, Properties, Public fields
- camelCase for: Local variables, Parameters
- _camelCase for: Private fields
- UPPER_CASE for: Constants
- IPascalCase for: Interfaces

### Async Methods
- Always suffix with `Async`: `GetByIdAsync`, `SaveAsync`
- Always return `Task` or `Task<T>`
- Always accept `CancellationToken` as last parameter

### Use Case Naming
- Commands: `{Verb}{Noun}Command` (e.g., `UploadVideoCommand`)
- Queries: `Get{Noun}Query` (e.g., `GetVideoQuery`)
- Handlers: `{CommandName}Handler` (e.g., `UploadVideoCommandHandler`)

## Environment Setup

### Required Tools
- .NET 9 SDK
- PostgreSQL 14+
- FFmpeg (for video processing)
- Docker (optional, for containerization)

### Optional Tools
- Visual Studio 2022 / VS Code / Rider
- Postman / Insomnia (API testing)
- pgAdmin (PostgreSQL GUI)
- Git

### Configuration Files
```
appsettings.json              # Default settings
appsettings.Development.json  # Development overrides
appsettings.Production.json   # Production settings (gitignored)
```

## Useful NuGet Packages

### Domain
- None (keep it pure!)

### Application
- MediatR
- FluentValidation
- AutoMapper

### Infrastructure
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL
- Hangfire or Quartz.NET
- Microsoft.AspNetCore.Authentication.JwtBearer

### API
- Swashbuckle.AspNetCore (Swagger)
- Serilog.AspNetCore
- Microsoft.AspNetCore.Authentication.JwtBearer

## Git Workflow

```bash
# Create feature branch
git checkout -b feature/video-upload

# Stage changes
git add .

# Commit with descriptive message
git commit -m "feat: implement video upload endpoint"

# Push to remote
git push origin feature/video-upload
```

## Troubleshooting

### Build Errors
```bash
# Clean solution
dotnet clean

# Restore packages
dotnet restore

# Rebuild
dotnet build
```

### Migration Issues
```bash
# Drop database (WARNING: data loss!)
dotnet ef database drop --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api

# Recreate from scratch
dotnet ef database update --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api
```

## Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
