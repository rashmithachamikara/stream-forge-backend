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
}
