using Microsoft.EntityFrameworkCore.Storage;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;
using StreamForge.Infrastructure.Persistence.Repositories;

namespace StreamForge.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly StreamForgeDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(StreamForgeDbContext dbContext)
    {
        _dbContext = dbContext;
        Users = new UserRepository(dbContext);
        Videos = new VideoRepository(dbContext);
        VideoVersions = new VideoVersionRepository(dbContext);
        VideoFiles = new VideoFileRepository(dbContext);
        VideoThumbnails = new VideoThumbnailRepository(dbContext);
        VideoProcessingJobs = new VideoProcessingJobRepository(dbContext);
        VideoTranscriptions = new VideoTranscriptionRepository(dbContext);
        VideoTranscriptChunks = new VideoTranscriptChunkRepository(dbContext);
        VideoReactions = new VideoReactionRepository(dbContext);
        VideoComments = new VideoCommentRepository(dbContext);
        Bookmarks = new BookmarkRepository(dbContext);
        VideoTags = new VideoTagRepository(dbContext);
        StorageProviders = new StorageProviderRepository(dbContext);
        Categories = new CategoryRepository(dbContext);
        Tags = new TagRepository(dbContext);
        Playlists = new PlaylistRepository(dbContext);
        PlaylistVideos = new PlaylistVideoRepository(dbContext);
        Notifications = new NotificationRepository(dbContext);
        AnalyticsEvents = new AnalyticsEventRepository(dbContext);
        SystemSettings = new SystemSettingRepository(dbContext);
        SystemSecrets = new SystemSecretRepository(dbContext);
        AccessControls = new AccessControlRepository(dbContext);
        UploadSessions = new UploadSessionRepository(dbContext);
        UploadSessionParts = new UploadSessionPartRepository(dbContext);
    }

    public IUserRepository Users { get; }
    public IVideoRepository Videos { get; }
    public IVideoVersionRepository VideoVersions { get; }
    public IVideoFileRepository VideoFiles { get; }
    public IVideoThumbnailRepository VideoThumbnails { get; }
    public IVideoProcessingJobRepository VideoProcessingJobs { get; }
    public IVideoTranscriptionRepository VideoTranscriptions { get; }
    public IVideoTranscriptChunkRepository VideoTranscriptChunks { get; }
    public IVideoReactionRepository VideoReactions { get; }
    public IVideoCommentRepository VideoComments { get; }
    public IBookmarkRepository Bookmarks { get; }
    public IVideoTagRepository VideoTags { get; }
    public IStorageProviderRepository StorageProviders { get; }
    public ICategoryRepository Categories { get; }
    public ITagRepository Tags { get; }
    public IPlaylistRepository Playlists { get; }
    public IPlaylistVideoRepository PlaylistVideos { get; }
    public INotificationRepository Notifications { get; }
    public IAnalyticsEventRepository AnalyticsEvents { get; }
    public ISystemSettingRepository SystemSettings { get; }
    public ISystemSecretRepository SystemSecrets { get; }
    public IAccessControlRepository AccessControls { get; }
    public IUploadSessionRepository UploadSessions { get; }
    public IUploadSessionPartRepository UploadSessionParts { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction ??= await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext.Dispose();
    }
}
