# Stream Forge Domain Layer - Quick Reference

## 📁 File Count Summary
- **Entities**: 20 files (BaseEntity + 19 domain entities)
- **Enumerations**: 12 files
- **Repository Interfaces**: 9 files
- **Domain Exceptions**: 8 files
- **Total Domain Files**: 49

## 📋 Complete File List

### Entities (src/StreamForge.Domain/Entities/)
1. BaseEntity.cs
2. User.cs
3. Category.cs
4. Tag.cs
5. StorageProvider.cs
6. Video.cs
7. VideoVersion.cs
8. VideoFile.cs
9. VideoThumbnail.cs
10. VideoProcessingJob.cs
11. VideoTranscription.cs
12. VideoTag.cs
13. VideoReaction.cs
14. VideoComment.cs
15. Bookmark.cs
16. Playlist.cs
17. PlaylistVideo.cs
18. AccessControl.cs
19. Notification.cs
20. AnalyticsEvent.cs

### Enumerations (src/StreamForge.Domain/Enums/)
1. UserRole.cs
2. VideoVisibility.cs
3. StorageProviderType.cs
4. VideoFormat.cs
5. ProcessingJobType.cs
6. ProcessingJobStatus.cs
7. TranscriptionStatus.cs
8. ReactionType.cs
9. PermissionType.cs
10. PlaylistVisibility.cs
11. AnalyticsEventType.cs
12. NotificationType.cs

### Repository Interfaces (src/StreamForge.Domain/Interfaces/)
1. IRepository.cs
2. IUserRepository.cs
3. IVideoRepository.cs
4. ICategoryRepository.cs
5. ITagRepository.cs
6. IPlaylistRepository.cs
7. INotificationRepository.cs
8. IAnalyticsEventRepository.cs
9. IUnitOfWork.cs

### Domain Exceptions (src/StreamForge.Domain/Exceptions/)
1. DomainException.cs
2. EntityNotFoundException.cs
3. VideoNotFoundException.cs
4. UserNotFoundException.cs
5. ValidationException.cs
6. UnauthorizedAccessException.cs
7. DuplicateEntityException.cs
8. BusinessRuleViolationException.cs

## 🎯 Key Features

### All Entities Include
✅ Private parameterless constructor for EF Core  
✅ Static `Create()` factory methods with validation  
✅ Business logic methods (Update, Increment, etc.)  
✅ Navigation properties properly initialized  
✅ Private setters for encapsulation  
✅ XML documentation comments  

### All Repository Interfaces Include
✅ Async/await patterns throughout  
✅ CancellationToken support  
✅ Entity-specific query methods  
✅ Generic base repository for CRUD  

### Domain Exceptions Hierarchy
```
Exception
  └─ DomainException (base)
      ├─ EntityNotFoundException (generic)
      │   ├─ VideoNotFoundException
      │   └─ UserNotFoundException
      ├─ ValidationException
      ├─ UnauthorizedAccessException
      ├─ DuplicateEntityException
      └─ BusinessRuleViolationException
```

## 🔗 Entity Relationship Summary

### Primary Aggregates
- **User**: Root aggregate for user-related operations
- **Video**: Root aggregate for video content and metadata
- **Playlist**: Root aggregate for video collections
- **Category**: Root aggregate for video taxonomy

### Join Tables (Many-to-Many)
- **VideoTag**: Videos ↔ Tags
- **PlaylistVideo**: Playlists ↔ Videos (with OrderIndex)

### Supporting Entities
- **VideoVersion**: Multiple formats/resolutions per video
- **VideoFile**: Physical storage references
- **VideoThumbnail**: Video preview images
- **VideoProcessingJob**: Background job tracking
- **VideoTranscription**: AI transcription data
- **VideoReaction**: User reactions (like/dislike)
- **VideoComment**: User comments with threading
- **Bookmark**: User video bookmarks
- **AccessControl**: Sharing and permissions
- **Notification**: User notifications
- **AnalyticsEvent**: Video playback analytics
- **StorageProvider**: Storage backend configuration

## 🚀 Usage Examples

### Creating an Entity
```csharp
var user = User.Create(
    email: "john@example.com",
    username: "johndoe",
    passwordHash: "hashed_password",
    fullName: "John Doe",
    role: UserRole.Viewer
);
```

### Updating an Entity
```csharp
user.UpdateProfile("john.doe@example.com", "John M. Doe");
user.ChangeRole(UserRole.Editor);
```

### Repository Usage
```csharp
// Get user by email
var user = await _userRepository.GetByEmailAsync("john@example.com");

// Search videos
var videos = await _videoRepository.SearchAsync("tutorial");

// Get most viewed videos
var popular = await _videoRepository.GetMostViewedAsync(10);
```

### Unit of Work Pattern
```csharp
await _unitOfWork.BeginTransactionAsync();
try
{
    var user = await _unitOfWork.Users.GetByIdAsync(userId);
    var video = Video.Create(...);
    await _unitOfWork.Videos.AddAsync(video);
    await _unitOfWork.SaveChangesAsync();
    await _unitOfWork.CommitTransactionAsync();
}
catch
{
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}
```

### Exception Handling
```csharp
// Throw domain exceptions
throw new VideoNotFoundException(videoId);
throw new ValidationException("Title", "Title cannot be empty");
throw new DuplicateEntityException("User", "Email", email);
throw new UnauthorizedAccessException("Video", videoId);
```

## 📊 Domain Statistics

| Category | Count |
|----------|-------|
| Total Entities | 19 |
| Enumerations | 12 |
| Repository Interfaces | 9 |
| Domain Exceptions | 8 |
| Navigation Properties | 50+ |
| Business Methods | 60+ |

## ✅ Clean Architecture Compliance

- ✅ **No external dependencies** (only .NET base libraries)
- ✅ **All business logic in domain** (no leakage to other layers)
- ✅ **Repository pattern** (data access abstraction)
- ✅ **Domain exceptions** (clear error handling)
- ✅ **Value objects ready** (folder prepared for future)
- ✅ **Aggregate roots identified** (User, Video, Playlist, Category)

## 🧪 Testing Readiness

The domain layer is structured for comprehensive testing:

### Unit Tests (Recommended)
- ✅ Entity creation validation
- ✅ Business method behavior
- ✅ Domain exception scenarios
- ✅ Navigation property integrity

### Integration Tests (Next Phase)
- ⏳ Repository implementations
- ⏳ Database queries
- ⏳ Transaction management
- ⏳ EF Core mappings

## 📖 Documentation Links

- [Schema v1.0 Specification](../documentation/schema/schema-v1.0.md)
- [Schema Diagrams](../documentation/schema/schema-diagram.md)
- [Migration Guide](../documentation/schema/migration-guide.md)
- [Domain Layer Summary](../documentation/DOMAIN_LAYER_SUMMARY.md)

---
**Status**: ✅ Complete  
**Build**: ✅ Successful  
**Ready For**: Infrastructure Layer Implementation
