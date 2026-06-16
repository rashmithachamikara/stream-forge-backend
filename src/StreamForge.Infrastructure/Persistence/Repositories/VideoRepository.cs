using Microsoft.EntityFrameworkCore;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence.Repositories;

public sealed class VideoRepository : BaseRepository<Video>, IVideoRepository
{
    public VideoRepository(StreamForgeDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Video?> GetWithDetailsAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        DbSet
            .AsNoTracking()
            .Include(video => video.Uploader)
            .Include(video => video.Category)
            .Include(video => video.VideoVersions)
            .Include(video => video.VideoThumbnails)
            .Include(video => video.VideoTags)
                .ThenInclude(videoTag => videoTag.Tag)
            .FirstOrDefaultAsync(video => video.Id == videoId, cancellationToken);

    public async Task<PagedQueryResult<Video>> SearchVisibleAsync(
        string? searchTerm,
        Guid? categoryId,
        Guid? tagId,
        Guid? uploaderId,
        VideoStatus? status,
        VideoVisibility? visibility,
        Guid? currentUserId,
        UserRole? currentUserRole,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(video => video.Uploader)
            .Include(video => video.Category)
            .Include(video => video.VideoVersions)
            .Include(video => video.VideoThumbnails)
            .Include(video => video.VideoTags)
                .ThenInclude(videoTag => videoTag.Tag)
            .AsQueryable();

        if (currentUserRole != UserRole.Admin)
        {
            query = query.Where(video => video.Status == VideoStatus.Ready);
            query = currentUserId.HasValue
                ? query.Where(video =>
                    video.Visibility == VideoVisibility.Public ||
                    video.Visibility == VideoVisibility.Internal ||
                    video.UploaderId == currentUserId.Value ||
                    video.AccessControls.Any(accessControl =>
                        accessControl.IsActive &&
                        (!accessControl.ExpiresAt.HasValue || accessControl.ExpiresAt > DateTime.UtcNow) &&
                        accessControl.UserId == currentUserId.Value &&
                        (accessControl.PermissionType == PermissionType.View ||
                         accessControl.PermissionType == PermissionType.Embed ||
                         accessControl.PermissionType == PermissionType.Download)))
                : query.Where(video => video.Visibility == VideoVisibility.Public);
        }
        else if (status.HasValue)
        {
            query = query.Where(video => video.Status == status.Value);
        }
        else
        {
            query = query.Where(video => video.Status != VideoStatus.Deleted);
        }

        if (currentUserRole == UserRole.Admin && visibility.HasValue)
        {
            query = query.Where(video => video.Visibility == visibility.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(video => video.CategoryId == categoryId.Value);
        }

        if (tagId.HasValue)
        {
            query = query.Where(video => video.VideoTags.Any(videoTag => videoTag.TagId == tagId.Value));
        }

        if (uploaderId.HasValue)
        {
            query = query.Where(video => video.UploaderId == uploaderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(video =>
                EF.Functions.ILike(video.Title, pattern) ||
                (video.Description != null && EF.Functions.ILike(video.Description, pattern)));
        }

        return await ToPagedResultAsync(ApplySort(query, sort), page, pageSize, cancellationToken);
    }

    public async Task<PagedQueryResult<Video>> GetUserLibraryAsync(
        Guid userId,
        VideoStatus? status,
        VideoVisibility? visibility,
        string? searchTerm,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(video => video.Uploader)
            .Include(video => video.Category)
            .Include(video => video.VideoVersions)
            .Include(video => video.VideoThumbnails)
            .Include(video => video.VideoTags)
                .ThenInclude(videoTag => videoTag.Tag)
            .Where(video => video.UploaderId == userId);

        if (status.HasValue)
        {
            query = query.Where(video => video.Status == status.Value);
        }

        if (visibility.HasValue)
        {
            query = query.Where(video => video.Visibility == visibility.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(video =>
                EF.Functions.ILike(video.Title, pattern) ||
                (video.Description != null && EF.Functions.ILike(video.Description, pattern)));
        }

        return await ToPagedResultAsync(ApplySort(query, sort), page, pageSize, cancellationToken);
    }

    public async Task<IEnumerable<Video>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(video => video.UploaderId == userId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Video>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(video => video.CategoryId == categoryId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Video>> GetByTagIdAsync(Guid tagId, CancellationToken cancellationToken = default) =>
        await DbSet.Where(video => video.VideoTags.Any(videoTag => videoTag.TagId == tagId)).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Video>> GetByVisibilityAsync(VideoVisibility visibility, CancellationToken cancellationToken = default) =>
        await DbSet.Where(video => video.Visibility == visibility).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Video>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default) =>
        await DbSet.Where(video => video.Title.Contains(searchTerm) || (video.Description != null && video.Description.Contains(searchTerm)))
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Video>> GetMostViewedAsync(int count, CancellationToken cancellationToken = default) =>
        await DbSet.OrderByDescending(video => video.ViewCount).Take(count).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Video>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        await DbSet.OrderByDescending(video => video.CreatedAt).Take(count).ToListAsync(cancellationToken);

    private static IQueryable<Video> ApplySort(IQueryable<Video> query, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(video => video.CreatedAt).ThenBy(video => video.Id),
            "title" => query.OrderBy(video => video.Title).ThenBy(video => video.Id),
            "views" => query.OrderByDescending(video => video.ViewCount).ThenByDescending(video => video.Id),
            _ => query.OrderByDescending(video => video.CreatedAt).ThenByDescending(video => video.Id)
        };
    }

    private static async Task<PagedQueryResult<Video>> ToPagedResultAsync(
        IQueryable<Video> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<Video>(items, totalCount, page, pageSize);
    }
}
