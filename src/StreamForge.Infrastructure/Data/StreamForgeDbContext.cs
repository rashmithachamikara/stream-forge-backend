using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data;

/// <summary>
/// Main database context for Stream Forge
/// </summary>
public class StreamForgeDbContext : DbContext
{
    public StreamForgeDbContext(DbContextOptions<StreamForgeDbContext> options)
        : base(options)
    {
    }

    // Core Entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<StorageProvider> StorageProviders => Set<StorageProvider>();

    // Video Entities
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<VideoVersion> VideoVersions => Set<VideoVersion>();
    public DbSet<VideoFile> VideoFiles => Set<VideoFile>();
    public DbSet<VideoThumbnail> VideoThumbnails => Set<VideoThumbnail>();
    public DbSet<VideoProcessingJob> VideoProcessingJobs => Set<VideoProcessingJob>();
    public DbSet<VideoTranscription> VideoTranscriptions => Set<VideoTranscription>();

    // Video Relations
    public DbSet<VideoTag> VideoTags => Set<VideoTag>();
    public DbSet<VideoReaction> VideoReactions => Set<VideoReaction>();
    public DbSet<VideoComment> VideoComments => Set<VideoComment>();

    // User Relations
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistVideo> PlaylistVideos => Set<PlaylistVideo>();
    public DbSet<AccessControl> AccessControls => Set<AccessControl>();

    // System Entities
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    // Upload Entities
    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();
    public DbSet<UploadSessionPart> UploadSessionParts => Set<UploadSessionPart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StreamForgeDbContext).Assembly);
    }
}
