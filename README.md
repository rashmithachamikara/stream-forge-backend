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

6. Health endpoints

```text
GET /health/live
GET /health/ready
```

`/health/live` confirms the process is up. `/health/ready` checks PostgreSQL connectivity and that the configured upload storage path is writable.

## Docker Deployment

The repo includes:

- `Dockerfile` for the API runtime
- `compose.yaml` with `api` and `postgres` services
- `compose.transcription.yaml` for the optional Python transcription worker
- `compose.host-paths.yaml` for optional bind mounts to host folders
- `.env.example` for deployment-time environment values

### Quick Start

1. Create a deployment env file

```bash
cp .env.example .env
```

2. Set at least a real `STREAMFORGE_JWT_SIGNING_KEY` and PostgreSQL password in `.env`

3. Start PostgreSQL

```bash
docker compose up -d postgres
```

4. Choose a migration strategy

Default, explicit migration step:

```bash
dotnet ef database update --project src/StreamForge.Infrastructure --startup-project src/StreamForge.Api
```

Optional startup auto-migration:

- Set `STREAMFORGE_DB_APPLY_MIGRATIONS_ON_STARTUP=true` in `.env`
- Then the API container can apply migrations itself on startup

5. Start the API container

```bash
docker compose up -d api
```

6. Verify readiness

```bash
curl http://localhost:8080/health/ready
```

### Optional Transcription Worker

To run the local Python transcription worker alongside the core stack, add the transcription compose layer:

```bash
docker compose -f compose.yaml -f compose.transcription.yaml up -d
```

This adds:

- `transcription-worker` on port `8090`
- shared media and transcription-output volumes between the API and worker
- API overrides so transcription submission uses `http://transcription-worker:8090`
- API callback override so the worker calls back to `http://api:8080`

If you want transcription enabled in Docker, keep these env values set appropriately in `.env`:

```text
STREAMFORGE_TRANSCRIPTION_ENABLED=true
STREAMFORGE_TRANSCRIPTION_AUTO_ON_READY=true
```

### Notes

- The container image includes `ffmpeg`, `ffprobe`, and `curl`.
- By default, PostgreSQL and media files are stored in named Docker volumes.
- The current deployment model uses local filesystem media storage, so horizontal scaling is limited until a shared or remote storage provider is added.
- The app now supports startup migration behavior through the `Database` settings, but the default remains conservative:
  - `ApplyMigrationsOnStartup=false`
  - `SeedOnStartup=true`
  - `WarnOnPendingMigrations=true`
- Production deployments should usually keep auto-migration off and run migrations explicitly.

### Optional Host Path Mounts

If you want to inspect files directly on your machine instead of using named Docker volumes, start the stack with the host-path override:

```bash
docker compose -f compose.yaml -f compose.host-paths.yaml up -d
```

If you want both host-path mounts and the transcription worker:

```bash
docker compose -f compose.yaml -f compose.transcription.yaml -f compose.host-paths.yaml up -d
```

By default, the override maps:

- `./docker-data/postgres` -> PostgreSQL data directory
- `./docker-data/uploads` -> uploaded and generated media
- `./docker-data/transcription-output` -> staged worker transcription artifacts
- `./docker-data/transcription-models` -> downloaded Whisper/model cache

You can change those paths through `.env`:

```text
STREAMFORGE_POSTGRES_HOST_PATH=./docker-data/postgres
STREAMFORGE_MEDIA_HOST_PATH=./docker-data/uploads
STREAMFORGE_TRANSCRIPTION_OUTPUT_HOST_PATH=./docker-data/transcription-output
STREAMFORGE_TRANSCRIPTION_MODEL_CACHE_HOST_PATH=./docker-data/transcription-models
```

Use the base `compose.yaml` alone when you want the default named-volume setup.

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

API integration tests also live in a separate opt-in project:
  - `StreamForge.Api.IntegrationTests`

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

Run the API integration suite with Docker/Testcontainers enabled:

```powershell
$env:STREAMFORGE_RUN_API_INTEGRATION_TESTS='true'
dotnet test tests/StreamForge.Api.IntegrationTests/StreamForge.Api.IntegrationTests.csproj --filter "Category=ApiIntegration"
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

This repository is proprietary and not licensed for public use, redistribution, or modification without prior written permission.
See [LICENSE](C:/Files/Shared/Software%20Projects/Stream%20Forge/stream-forge-backend/LICENSE:1).

## Contact

[Your Contact Information]
