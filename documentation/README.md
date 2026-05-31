# Stream Forge Documentation

Welcome to the Stream Forge documentation. This directory contains comprehensive technical documentation for the project.

## 📚 Documentation Structure

### Schema Documentation
**Location:** [`schema/`](schema/)

Complete database schema specification and design documentation.

- **[schema-v1.0.md](schema/schema-v1.0.md)** - Full database schema specification
- **[schema-diagram.md](schema/schema-diagram.md)** - Entity relationship diagrams
- **[migration-guide.md](schema/migration-guide.md)** - Database migration procedures
- **[README.md](schema/README.md)** - Schema documentation index

### Architecture Documentation
**Location:** Root project files

Project structure and architectural decisions.

- **[../STRUCTURE.md](../STRUCTURE.md)** - Complete folder structure
- **[../README.md](../README.md)** - Project overview
- **[../QUICK_REFERENCE.md](../QUICK_REFERENCE.md)** - Developer quick reference

## 🎯 Quick Start

### For Developers

1. **Understand the Architecture**
   - Read [STRUCTURE.md](../STRUCTURE.md) for Clean Architecture overview
   - Review [schema-v1.0.md](schema/schema-v1.0.md) for database design

2. **Set Up Development Environment**
   - Follow [QUICK_REFERENCE.md](../QUICK_REFERENCE.md) setup instructions
   - Review [migration-guide.md](schema/migration-guide.md) for database setup

3. **Start Coding**
   - Use [QUICK_REFERENCE.md](../QUICK_REFERENCE.md) for common patterns
   - Reference schema documentation for entity design

### For Database Administrators

1. **Schema Overview**
   - Review [schema-v1.0.md](schema/schema-v1.0.md) - Complete schema spec
   - Study [schema-diagram.md](schema/schema-diagram.md) - Visual relationships

2. **Migration Procedures**
   - Follow [migration-guide.md](schema/migration-guide.md) for all migrations
   - Use provided scripts for backup and rollback

### For Project Managers

1. **Project Status**
   - Check [../EXECUTION_PLAN.md](../EXECUTION_PLAN.md) for current phase status
   - Review [../README.md](../README.md) for feature overview

2. **Development Roadmap**
   - See [../EXECUTION_PLAN.md](../EXECUTION_PLAN.md) for execution phases
   - See [plans/](plans/) for phase-specific details
   - Track progress against schema implementation phases

## 📖 Documentation Index

### Database Schema (v1.0)

#### Core Tables (7)
1. **Users** - User accounts and authentication
2. **Videos** - Video metadata and settings
3. **VideoVersions** - Multiple resolutions/formats
4. **VideoFiles** - Physical file storage references
5. **VideoThumbnails** - Thumbnail images
6. **StorageProviders** - Storage backend configuration
7. **VideoProcessingJobs** - Background processing tasks

#### Organization (3)
8. **Categories** - Hierarchical video categories
9. **Tags** - Video tagging system
10. **VideoTags** - Many-to-many join table

#### Engagement (5)
11. **VideoReactions** - Likes/dislikes
12. **VideoComments** - Comments with replies
13. **Bookmarks** - User bookmarks
14. **Playlists** - User playlists
15. **PlaylistVideos** - Many-to-many join table

#### Advanced (4)
16. **VideoTranscriptions** - AI transcriptions
17. **AccessControl** - Sharing and permissions
18. **Notifications** - User notifications
19. **AnalyticsEvents** - Analytics tracking

### Key Architectural Decisions

#### ✅ Implemented in v1.0
- Removed separate Roles table (using enum)
- Merged VideoSettings into Videos table
- Split Feedback into VideoReactions and VideoComments
- Added VideoThumbnails for explicit thumbnail tracking
- Added VideoProcessingJobs for background task tracking
- Added VideoTranscriptions for AI features
- Added SessionId to AnalyticsEvents for session grouping
- Enhanced AccessControl with ShareToken for secure sharing

#### 🎨 Design Patterns
- **Repository Pattern** - Data access abstraction
- **CQRS** - Command/Query separation via MediatR
- **Strategy Pattern** - Pluggable storage providers
- **Factory Pattern** - Video processing job creation
- **Specification Pattern** - Complex query building (optional)

#### 🔐 Security Considerations
- JWT authentication
- Role-based authorization (Admin, Editor, Viewer)
- Token-based sharing (ShareToken)
- Password hashing (not stored plain text)
- Access control with expiration

#### ⚡ Performance Optimizations
- Denormalized counts (ViewCount, VideoCount, UsageCount)
- Strategic indexing on foreign keys and frequently queried columns
- Composite indexes for join tables
- Analytics event partitioning strategy
- Caching strategy for metadata and thumbnails

## 🔄 Schema Evolution

### Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | Jan 22, 2026 | Initial schema with all recommendations implemented |

### Future Enhancements

#### Phase 1 (MVP) - In Progress
- Core video management
- User authentication
- Basic engagement features
- Local storage

#### Phase 2 (Post-MVP) - Planned
- AI transcription integration
- Advanced analytics
- Cloud storage providers
- Real-time notifications

#### Phase 3 (Advanced) - Future
- Live streaming support
- CDN integration
- Advanced AI features
- Multi-tenancy

## 🛠️ Tools and Technologies

### Database
- **PostgreSQL 14+** - Primary database
- **Entity Framework Core** - ORM
- **Npgsql** - PostgreSQL provider

### Development
- **.NET 9** - Framework
- **ASP.NET Core** - Web API
- **MediatR** - CQRS implementation
- **FluentValidation** - Request validation
- **AutoMapper** - Object mapping

### Background Processing
- **Hangfire** or **Quartz.NET** - Job scheduling
- **FFmpeg** - Video processing

### Storage
- **Local Filesystem** - Initial implementation
- **AWS S3** - Future support
- **Azure Blob Storage** - Future support

## 📝 Contributing to Documentation

### Adding New Documentation

1. Create markdown file in appropriate directory
2. Follow existing naming conventions
3. Update this README.md index
4. Add cross-references to related documents

### Documentation Standards

- Use clear, descriptive headings
- Include code examples where applicable
- Add diagrams for complex concepts
- Keep language concise and professional
- Update version history and dates

### File Naming Convention

```
{category}-{description}-{version}.md

Examples:
- schema-v1.0.md
- api-endpoints-v1.0.md
- deployment-guide-v1.0.md
```

## 🔗 External Resources

### Entity Framework Core
- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [PostgreSQL Provider](https://www.npgsql.org/efcore/)
- [Migrations Overview](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

### Clean Architecture
- [Clean Architecture Book](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)

### Video Processing
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
- [HLS Streaming Guide](https://developer.apple.com/streaming/)
- [DASH Protocol](https://dashif.org/)

### PostgreSQL
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Performance Tuning](https://wiki.postgresql.org/wiki/Performance_Optimization)
- [Indexing Strategies](https://www.postgresql.org/docs/current/indexes.html)

## 📧 Support

For questions or clarifications about the documentation:

1. Check existing documentation first
2. Review related diagrams and examples
3. Consult external resources linked above
4. Reach out to the development team

---

**Documentation Version:** 1.0  
**Last Updated:** January 22, 2026  
**Maintained By:** Stream Forge Development Team
