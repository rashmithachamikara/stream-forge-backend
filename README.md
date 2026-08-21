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

The complete local stack includes PostgreSQL, the API, transcription and embedding workers, and the Next.js frontend. `compose.frontend.yaml` expects the repositories to be sibling directories named `stream-forge-backend` and `stream-forge-frontend`.

1. Copy `.env.example` to `.env` and replace at least `POSTGRES_PASSWORD`, `STREAMFORGE_JWT_SIGNING_KEY`, `STREAMFORGE_TRANSCRIPTION_CALLBACK_SECRET`, and `STREAMFORGE_SEED_ADMIN_PASSWORD` with strong values.
2. Validate the combined configuration:

```bash
docker compose -f compose.full.yaml -f compose.frontend.yaml config
```

3. Start PostgreSQL and apply EF Core migrations:

```bash
docker compose -f compose.full.yaml -f compose.frontend.yaml up -d postgres
docker compose -f compose.full.yaml -f compose.frontend.yaml --profile migration run --rm migrate
```

4. Build and start the complete project:

```bash
docker compose -f compose.full.yaml -f compose.frontend.yaml up -d --build
```

Open the frontend at `http://localhost:3000`; the API and readiness endpoint are at `http://localhost:8080` and `http://localhost:8080/health/ready`. `STREAMFORGE_PUBLIC_API_URL` is embedded in the frontend during its image build, so rebuild the frontend after changing it.

`STREAMFORGE_SEED_ADMIN_PASSWORD` creates the initial administrator when the configured email does not already exist; it never resets an existing account password.

Use `docker compose -f compose.full.yaml -f compose.frontend.yaml down` to stop the stack without deleting named-volume data. Do not add `-v` unless the database and other persisted data should also be removed.

For smaller backend-only stacks, use `compose.yaml` by itself or combine it with `compose.transcription.yaml` and/or `compose.embedding.yaml`; these variants retain named-volume storage.

### Deploy using registry images

To deploy without either source repository, copy `.env.example` to `.env` and replace at least `POSTGRES_PASSWORD`, `STREAMFORGE_JWT_SIGNING_KEY`, `STREAMFORGE_TRANSCRIPTION_CALLBACK_SECRET`, and `STREAMFORGE_SEED_ADMIN_PASSWORD` with strong values.

```bash
docker compose -f compose.registry.yaml pull
docker compose -f compose.registry.yaml up -d
```

The registry deployment applies pending migrations during API startup because the published runtime image does not contain the .NET SDK or EF CLI.

### Complete stack with host paths

Append the same host-path override to either complete stack:

```bash
docker compose -f compose.full.yaml -f compose.frontend.yaml -f compose.host-paths.yaml up -d --build
docker compose -f compose.registry.yaml -f compose.host-paths.yaml up -d
```

The override maps PostgreSQL, shared API/transcription data, Data Protection keys, and both worker model caches to the paths configured in `.env`. It is a complete-stack override and should not be combined with core-only `compose.yaml`.

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
