# Database Schema Documentation

This directory contains the complete database schema documentation for Stream Forge.

## Files

- **[schema-v7.0.md](schema-v7.0.md)** - Current database schema specification (v7.0)
- **[schema-v6.0.md](schema-v6.0.md)** - Previous schema version with lexical transcript search
- **[schema-v5.0.md](schema-v5.0.md)** - Previous schema version with transcript chunk persistence and system settings foundation
- **[schema-v4.0.md](schema-v4.0.md)** - Previous schema version with transcription job metadata and caption delivery alignment
- **[schema-v3.0.md](schema-v3.0.md)** - Earlier schema version with bookmark redesign and initial Phase 15 alignment
- **[schema-v1.0.md](schema-v1.0.md)** - Complete database schema specification (v1.0)
- **[schema-diagram.md](schema-diagram.md)** - Visual entity relationship diagrams
- **[migration-guide.md](migration-guide.md)** - Database migration guidelines

## Quick Reference

### Total Tables: 21

#### Core Tables (7)
1. Users
2. Videos
3. VideoVersions
4. VideoFiles
5. VideoThumbnails
6. StorageProviders
7. VideoProcessingJobs

#### Organization Tables (3)
8. Categories
9. Tags
10. VideoTags (join table)

#### Engagement Tables (4)
11. VideoReactions
12. VideoComments
13. Bookmarks
14. Playlists
15. PlaylistVideos (join table)

#### Advanced Features (6)
16. VideoTranscriptions
17. VideoTranscriptChunks
18. SystemSettings
19. AccessControl
20. Notifications
21. AnalyticsEvents

## Implementation Status

| Phase | Status | Tables |
|-------|--------|--------|
| Phase 1 (MVP) | 🟡 Ready | 15 core tables |
| Phase 2 (Post-MVP) | ⏳ Planned | 4 advanced tables |

## Schema Principles

### Clean Architecture Compliance
- ✅ Domain entities match database tables
- ✅ No EF Core annotations in Domain layer
- ✅ Fluent API configurations in Infrastructure layer
- ✅ Repository pattern for data access

### Performance Optimizations
- ✅ Strategic indexing on foreign keys
- ✅ Composite unique indexes for join tables
- ✅ Denormalized counts for performance
- ✅ Partitioning strategy for AnalyticsEvents

### Data Integrity
- ✅ Foreign key constraints
- ✅ Unique constraints where needed
- ✅ NOT NULL constraints
- ✅ Default values
- ✅ Cascading deletes configured

## Key Design Decisions

### 1. Role Management
**Decision:** Use enum in Users table instead of separate Roles table.
**Rationale:** Simpler for MVP, three fixed roles only.
**Future:** Can migrate to many-to-many if needed.

### 2. Video Settings
**Decision:** Merged into Videos table.
**Rationale:** 1:1 relationship, fewer joins.

### 3. Feedback Split
**Decision:** Separate VideoReactions and VideoComments tables.
**Rationale:** Different access patterns, cleaner queries.

### 4. Analytics Storage
**Decision:** Single AnalyticsEvents table with SessionId.
**Rationale:** Simple to start, can partition or migrate to TimescaleDB later.

### 5. Storage Abstraction
**Decision:** Three-layer storage (StorageProviders → VideoFiles → VideoVersions).
**Rationale:** Supports multiple providers, cloud migration, redundancy.

## Common Queries

### Get Video with All Metadata
```sql
SELECT v.*, u.Name as UploaderName, c.Name as CategoryName
FROM Videos v
JOIN Users u ON v.UploaderId = u.Id
LEFT JOIN Categories c ON v.CategoryId = c.Id
WHERE v.Id = @VideoId;
```

### Get Available Video Versions
```sql
SELECT vv.*, vf.FilePath
FROM VideoVersions vv
JOIN VideoFiles vf ON vv.Id = vf.VideoVersionId
WHERE vv.VideoId = @VideoId
ORDER BY vv.Resolution DESC;
```

### Get Video Reactions Count
```sql
SELECT 
    ReactionType,
    COUNT(*) as Count
FROM VideoReactions
WHERE VideoId = @VideoId
GROUP BY ReactionType;
```

### Get User's Playlists with Video Count
```sql
SELECT p.*, COUNT(pv.VideoId) as VideoCount
FROM Playlists p
LEFT JOIN PlaylistVideos pv ON p.Id = pv.PlaylistId
WHERE p.OwnerId = @UserId
GROUP BY p.Id;
```

## Related Documentation

- [STRUCTURE.md](../../STRUCTURE.md) - Project structure
- [QUICK_REFERENCE.md](../../QUICK_REFERENCE.md) - Development guide
- [README.md](../../README.md) - Project overview

---

**Last Updated:** July 1, 2026  
**Schema Version:** 7.0
