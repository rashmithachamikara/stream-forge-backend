# Stream Forge

A self-hosted video-on-demand (VOD) platform backend designed for organizational and enterprise use.

## Tech Stack

- **.NET 9** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful API
- **Clean Architecture** - Modular Monolith pattern
- **Entity Framework Core** - ORM for database access
- **PostgreSQL** - Primary database
- **JWT Authentication** - Secure token-based auth
- **Background Jobs** - Video processing (Hangfire/Quartz.NET)
- **HLS Streaming** - HTTP Live Streaming protocol
- **FFmpeg** - Video transcoding
- **Pluggable Storage** - Local, S3, Azure Blob support

## Architecture

This project follows **Clean Architecture** principles with strict dependency rules:

```
┌─────────────────────────────────────────┐
│            StreamForge.Api              │
│     (Controllers, Middleware, DTOs)     │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│      StreamForge.Infrastructure         │
│  (Persistence, Storage, Services, Jobs) │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│       StreamForge.Application           │
│     (Use Cases, Interfaces, DTOs)       │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│         StreamForge.Domain              │
│   (Entities, Value Objects, Enums)      │
│         NO DEPENDENCIES                 │
└─────────────────────────────────────────┘
```

### Dependency Rules

- **Domain** has no dependencies on other layers
- **Application** depends only on Domain
- **Infrastructure** depends on Application and Domain
- **API** depends on Application and Infrastructure (for DI setup only)

## Project Structure

```
stream-forge-backend/
├── src/
│   ├── StreamForge.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── ValueObjects/
│   │   ├── Exceptions/
│   │   └── Interfaces/
│   │
│   ├── StreamForge.Application/
│   │   ├── UseCases/
│   │   │   ├── Videos/
│   │   │   └── Authentication/
│   │   ├── Interfaces/
│   │   ├── DTOs/
│   │   └── Common/
│   │
│   ├── StreamForge.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   └── Repositories/
│   │   ├── Storage/
│   │   ├── BackgroundJobs/
│   │   ├── Services/
│   │   └── Authentication/
│   │
│   └── StreamForge.Api/
│       ├── Controllers/
│       ├── Middleware/
│       ├── Filters/
│       └── Extensions/
│
└── StreamForge.sln
```

## User Roles

### Administrator

- Manage users, videos, and system settings
- Access analytics and reports
- Configure storage providers

### Editor

- Upload and manage videos
- Edit metadata (title, description, tags, categories)
- Configure video permissions and visibility
- Generate embed codes and share links

### Viewer

- Watch videos
- Like/dislike content
- Comment on videos
- Bookmark videos
- Create playlists

## Core Features

### 1. Authentication & Authorization

- JWT-based authentication
- Role-based access control (RBAC)
- Token refresh mechanism

### 2. Video Management

- Video upload with chunked support
- Metadata management (title, description, tags, categories)
- Multiple video versions (resolutions, formats, bitrates)
- HLS streaming support
- Video settings (autoplay, visibility, comments)

### 3. Storage Abstraction

- `IStorageService` interface
- Local filesystem storage
- Future: S3-compatible storage
- Future: Azure Blob Storage

### 4. Video Processing

- Background job processing
- FFmpeg-based transcoding
- Automatic thumbnail generation
- Multiple resolution outputs
- HLS playlist generation

### 5. AI Features

- Video transcription
- Searchable text indexing
- Optional AI summarization

### 6. Sharing & Embedding

- Secure watch URLs
- Embeddable iframe codes
- Access control enforcement

### 7. Engagement

- Likes and dislikes
- Comments system
- In-video personal bookmarks with notes
- User playlists
- Notifications

### 8. Analytics

- View tracking
- Watch time metrics
- Per-video statistics
- User engagement metrics

## Database Schema

### Core Tables

- **Users** - User accounts
- **Roles** - User roles (Admin, Editor, Viewer)
- **Videos** - Video metadata
- **VideoVersions** - Different resolutions/formats
- **VideoFiles** - Physical file references
- **VideoSettings** - Playback and visibility settings
- **StorageProviders** - Storage configuration
- **Categories** - Video categories
- **Tags** - Video tags
- **AccessControl** - Video permissions
- **Playlists** - User playlists
- **Comments** - Video comments
- **Likes** - Video likes/dislikes
- **Bookmarks** - User-owned timestamp markers within videos
- **Notifications** - User notifications
- **AnalyticsEvents** - Tracking events

## Getting Started

### Prerequisites

- .NET 9 SDK
- PostgreSQL 14+
- FFmpeg (for video processing)

### Setup

1. Clone the repository

```bash
git clone <repository-url>
cd stream-forge-backend
```

2. Restore dependencies

```bash
dotnet restore
```

3. Update connection string in `appsettings.json`

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Database=streamforge;Username=postgres;Password=yourpassword"
    }
}
```

4. Run migrations

```bash
dotnet ef database update --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api
```

5. Run the application

```bash
dotnet run --project src/StreamForge.Api
```

## Development Guidelines

### SOLID Principles

- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable
- **Interface Segregation**: Many specific interfaces over one general
- **Dependency Inversion**: Depend on abstractions, not concretions

### Code Standards

- Use `async`/`await` for all I/O operations
- Use strongly-typed enums
- Use MediatR for use cases (CQRS pattern)
- Use EF Core Fluent API (no data annotations in domain)
- DTOs only in Application and API layers
- Add XML documentation comments for public APIs

### Testing

Test projects live under `tests/` and are included in `StreamForge.sln`:
  - `StreamForge.Domain.Tests`
  - `StreamForge.Application.Tests`
  - `StreamForge.Infrastructure.Tests`
  - `StreamForge.Api.Tests`

PostgreSQL-backed integration tests live in a separate opt-in project:
  - `StreamForge.Infrastructure.IntegrationTests`

Run the default test suite:

```bash
dotnet test StreamForge.sln --no-restore
```

The default suite includes domain, application, EF model configuration, and non-Docker tests.

Run the PostgreSQL-backed integration suite with Docker/Testcontainers enabled:

```powershell
$env:STREAMFORGE_RUN_POSTGRES_TESTS='true'
dotnet test tests/StreamForge.Infrastructure.IntegrationTests/StreamForge.Infrastructure.IntegrationTests.csproj --filter "Category=PostgresIntegration"
```

After cloning or after package changes, restore first:

```bash
dotnet restore StreamForge.sln
dotnet test StreamForge.sln --no-restore
```

## Future Enhancements

- Real-time streaming (RTMP)
- Live streaming support
- CDN integration
- Advanced AI features (scene detection, auto-tagging)
- Mobile SDK
- Video editing capabilities

## License

[Your License]

## Contact

[Your Contact Information]
