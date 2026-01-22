# Stream Forge - Complete Folder Structure

```
stream-forge-backend/
│
├── .editorconfig                          # Code formatting rules
├── .gitignore                             # Git ignore rules
├── README.md                              # Project documentation
├── StreamForge.sln                        # Solution file
│
└── src/                                   # Source code directory
    │
    ├── StreamForge.Domain/                # Domain Layer (Core Business Logic)
    │   ├── Entities/                      # Domain entities (User, Video, etc.)
    │   ├── Enums/                         # Enumerations (UserRole, VideoStatus, etc.)
    │   ├── ValueObjects/                  # Value objects (immutable types)
    │   ├── Exceptions/                    # Domain-specific exceptions
    │   ├── Interfaces/                    # Domain interfaces (IRepository, etc.)
    │   └── StreamForge.Domain.csproj
    │
    ├── StreamForge.Application/           # Application Layer (Use Cases)
    │   ├── UseCases/                      # Business use cases
    │   │   ├── Videos/                    # Video-related use cases
    │   │   │   ├── UploadVideo/           # Upload video command
    │   │   │   ├── GetVideo/              # Get video query
    │   │   │   ├── DeleteVideo/           # Delete video command
    │   │   │   └── UpdateVideoMetadata/   # Update metadata command
    │   │   │
    │   │   └── Authentication/            # Auth-related use cases
    │   │       ├── Login/                 # Login command
    │   │       ├── Register/              # Register command
    │   │       └── RefreshToken/          # Token refresh command
    │   │
    │   ├── Interfaces/                    # Application interfaces
    │   │   ├── IStorageService.cs         # Storage abstraction
    │   │   ├── IVideoProcessingService.cs # Video processing abstraction
    │   │   ├── IAuthenticationService.cs  # Authentication abstraction
    │   │   └── INotificationService.cs    # Notification abstraction
    │   │
    │   ├── DTOs/                          # Data Transfer Objects
    │   │   ├── VideoDto.cs                # Video DTO
    │   │   ├── UserDto.cs                 # User DTO
    │   │   └── ...                        # Other DTOs
    │   │
    │   ├── Common/                        # Shared application code
    │   │   ├── Behaviors/                 # MediatR behaviors (validation, logging)
    │   │   ├── Mappings/                  # Object mapping profiles
    │   │   └── Exceptions/                # Application exceptions
    │   │
    │   └── StreamForge.Application.csproj
    │
    ├── StreamForge.Infrastructure/        # Infrastructure Layer (External Concerns)
    │   ├── Persistence/                   # Database implementation
    │   │   ├── Configurations/            # EF Core entity configurations
    │   │   │   ├── UserConfiguration.cs   # User entity config
    │   │   │   ├── VideoConfiguration.cs  # Video entity config
    │   │   │   └── ...                    # Other configurations
    │   │   │
    │   │   ├── Repositories/              # Repository implementations
    │   │   │   ├── VideoRepository.cs     # Video repository
    │   │   │   ├── UserRepository.cs      # User repository
    │   │   │   └── ...                    # Other repositories
    │   │   │
    │   │   ├── StreamForgeDbContext.cs    # Main DbContext
    │   │   └── Migrations/                # EF Core migrations
    │   │
    │   ├── Storage/                       # Storage implementations
    │   │   ├── LocalStorageService.cs     # Local filesystem storage
    │   │   ├── S3StorageService.cs        # AWS S3 storage (future)
    │   │   └── AzureBlobStorageService.cs # Azure Blob storage (future)
    │   │
    │   ├── BackgroundJobs/                # Background job implementations
    │   │   ├── VideoTranscodingJob.cs     # Video transcoding job
    │   │   ├── ThumbnailGenerationJob.cs  # Thumbnail generation job
    │   │   └── TranscriptionJob.cs        # AI transcription job
    │   │
    │   ├── Services/                      # External service implementations
    │   │   ├── VideoProcessingService.cs  # FFmpeg wrapper
    │   │   ├── EmailService.cs            # Email notifications
    │   │   └── AIService.cs               # AI transcription/summarization
    │   │
    │   ├── Authentication/                # Authentication implementation
    │   │   ├── JwtTokenService.cs         # JWT token generation
    │   │   └── PasswordHasher.cs          # Password hashing
    │   │
    │   └── StreamForge.Infrastructure.csproj
    │
    └── StreamForge.Api/                   # API Layer (HTTP Interface)
        ├── Controllers/                   # API controllers
        │   ├── VideosController.cs        # Video endpoints
        │   ├── AuthController.cs          # Authentication endpoints
        │   ├── UsersController.cs         # User management endpoints
        │   ├── PlaylistsController.cs     # Playlist endpoints
        │   ├── AnalyticsController.cs     # Analytics endpoints
        │   └── StreamingController.cs     # Video streaming endpoints
        │
        ├── Middleware/                    # Custom middleware
        │   ├── ExceptionHandlingMiddleware.cs  # Global error handling
        │   ├── RequestLoggingMiddleware.cs     # Request logging
        │   └── RateLimitingMiddleware.cs       # Rate limiting
        │
        ├── Filters/                       # Action filters
        │   ├── ValidateModelAttribute.cs  # Model validation filter
        │   └── AuthorizeRolesAttribute.cs # Role-based authorization
        │
        ├── Extensions/                    # Extension methods
        │   ├── ServiceCollectionExtensions.cs  # DI setup
        │   └── ApplicationBuilderExtensions.cs # Middleware setup
        │
        ├── Program.cs                     # Application entry point
        ├── appsettings.json              # Configuration
        ├── appsettings.Development.json  # Development config
        └── StreamForge.Api.csproj

```

## Key Architectural Decisions

### 1. **Dependency Flow** (Clean Architecture)
```
Domain ← Application ← Infrastructure
                    ↑
                   API
```

- **Domain**: No dependencies, pure business logic
- **Application**: Depends only on Domain, defines interfaces
- **Infrastructure**: Implements Application interfaces, depends on Domain
- **API**: Orchestrates, depends on Application & Infrastructure (for DI only)

### 2. **Project Purposes**

#### StreamForge.Domain
- Core business entities (User, Video, VideoVersion, etc.)
- Business rules and domain logic
- Domain events
- No framework dependencies

#### StreamForge.Application
- Use cases (Commands and Queries via MediatR)
- Application services interfaces
- DTOs for data transfer
- Validation logic
- No infrastructure concerns

#### StreamForge.Infrastructure
- Database access (EF Core)
- External services (Storage, Email, AI)
- Background jobs (Hangfire/Quartz)
- Authentication (JWT)
- All third-party integrations

#### StreamForge.Api
- REST API endpoints
- HTTP request/response handling
- Authentication/Authorization middleware
- API documentation (Swagger)
- Request validation

### 3. **Design Patterns Used**

- **CQRS** (Command Query Responsibility Segregation) via MediatR
- **Repository Pattern** for data access
- **Unit of Work** pattern in DbContext
- **Strategy Pattern** for pluggable storage
- **Factory Pattern** for video processing
- **Specification Pattern** for complex queries (optional)

### 4. **Key Technologies**

| Layer          | Technologies                                    |
|----------------|-------------------------------------------------|
| Domain         | Pure C# (no dependencies)                       |
| Application    | MediatR, FluentValidation, AutoMapper           |
| Infrastructure | EF Core, PostgreSQL, Hangfire, FFmpeg, JWT     |
| API            | ASP.NET Core, Swagger, Serilog                  |

### 5. **Data Flow Example: Upload Video**

1. **API Layer**: `VideosController` receives HTTP POST request
2. **Application Layer**: `UploadVideoCommand` handles business logic
3. **Domain Layer**: `Video` entity validates business rules
4. **Infrastructure Layer**: 
   - `VideoRepository` saves to database
   - `LocalStorageService` stores file
   - `VideoTranscodingJob` queued for background processing
5. **API Layer**: Returns 201 Created with video DTO

### 6. **Future Folders** (To Be Added)

```
├── tests/                           # Test projects
│   ├── StreamForge.Domain.Tests/
│   ├── StreamForge.Application.Tests/
│   ├── StreamForge.Infrastructure.Tests/
│   └── StreamForge.Api.Tests/
│
├── docs/                            # Documentation
│   ├── api/                         # API documentation
│   ├── architecture/                # Architecture diagrams
│   └── deployment/                  # Deployment guides
```

## Next Steps

1. ✅ Solution structure created
2. ✅ Project references configured
3. ✅ Folder structure established
4. ⏳ Implement core domain entities
5. ⏳ Create repository interfaces
6. ⏳ Implement DbContext and configurations
7. ⏳ Add authentication
8. ⏳ Create first use case (Upload Video)
9. ⏳ Implement storage abstraction
10. ⏳ Set up background jobs
