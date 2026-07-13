using System.Security.Cryptography;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Processing;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.UseCases.Content;

public sealed record ListVideosQuery(
    string? Search,
    Guid? CategoryId,
    Guid? TagId,
    Guid? UploaderId,
    Guid? ExcludeUploaderId,
    VideoStatus? Status,
    VideoVisibility? Visibility,
    string? Sort,
    int Page,
    int PageSize,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null);

public sealed record ListMyVideosQuery(
    VideoStatus? Status,
    VideoVisibility? Visibility,
    string? Search,
    string? Sort,
    int Page,
    int PageSize);

public sealed record ListMyUploadSessionsQuery(
    UploadSessionStatus? Status,
    int Page,
    int PageSize);

public sealed record ListTagsQuery(string? Search, int Page, int PageSize);

public sealed record ListUsersQuery(
    string? Search,
    UserRole? Role,
    bool? IsActive,
    int Page,
    int PageSize,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null);

public sealed record ListVideoAccessGrantsQuery(Guid VideoId, bool? IsActive, int Page, int PageSize);

public sealed class ListVideosService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListVideosService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponseDto<VideoSummaryDto>> Handle(ListVideosQuery query, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(query.Page);
        var pageSize = Pagination.NormalizePageSize(query.PageSize);
        var result = await _unitOfWork.Videos.SearchVisibleAsync(
            query.Search,
            query.CategoryId,
            query.TagId,
            query.UploaderId,
            query.ExcludeUploaderId,
            query.Status,
            query.Visibility,
            _currentUserService.UserId,
            _currentUserService.Role,
            query.Sort,
            page,
            pageSize,
            query.CreatedFrom,
            query.CreatedTo,
            cancellationToken);

        return Pagination.Map(result, video => ContentMapper.ToSummary(video));
    }
}

public sealed class GetVideoDetailsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public GetVideoDetailsService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<VideoDetailDto> Handle(Guid videoId, string? shareToken, CancellationToken cancellationToken)
    {
        if (!await _authorizationService.CanViewVideoAsync(videoId, _currentUserService.UserId, _currentUserService.Role, shareToken, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have access to this video");
        }

        var video = await _unitOfWork.Videos.GetWithDetailsAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);

        return ContentMapper.ToDetail(video);
    }
}

public sealed class ListMyVideosService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListMyVideosService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponseDto<VideoSummaryDto>> Handle(ListMyVideosQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        var page = Pagination.NormalizePage(query.Page);
        var pageSize = Pagination.NormalizePageSize(query.PageSize);
        var result = await _unitOfWork.Videos.GetUserLibraryAsync(
            userId,
            query.Status,
            query.Visibility,
            query.Search,
            query.Sort,
            page,
            pageSize,
            cancellationToken);

        return Pagination.Map(result, video => ContentMapper.ToSummary(video));
    }
}

public sealed class UpdateVideoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public UpdateVideoService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<VideoDetailDto> Handle(Guid videoId, UpdateVideoRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (!await _authorizationService.CanManageVideoAsync(videoId, userId, _currentUserService.Role, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have permission to manage this video");
        }

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);

        if (request.CategoryId.HasValue && !await _unitOfWork.Categories.ExistsAsync(request.CategoryId.Value, cancellationToken))
        {
            throw new EntityNotFoundException("Category", request.CategoryId.Value);
        }

        if (request.Title is not null || request.Description is not null || request.CategoryId.HasValue)
        {
            video.UpdateMetadata(
                request.Title ?? video.Title,
                request.Description ?? video.Description,
                request.CategoryId.HasValue ? request.CategoryId : video.CategoryId);
        }

        if (request.Visibility.HasValue)
        {
            video.UpdateVisibility(request.Visibility.Value);
        }

        if (request.AllowComments.HasValue || request.AllowLikes.HasValue)
        {
            video.UpdateEngagementSettings(
                request.AllowComments ?? video.AllowComments,
                request.AllowLikes ?? video.AllowLikes);
        }

        if (request.Autoplay.HasValue ||
            request.Loop.HasValue ||
            request.DefaultVolume.HasValue ||
            request.CaptionsEnabled.HasValue ||
            request.PlayerTheme is not null)
        {
            video.UpdatePlayerSettings(
                request.Autoplay ?? video.Autoplay,
                request.Loop ?? video.Loop,
                request.DefaultVolume ?? video.DefaultVolume,
                request.CaptionsEnabled ?? video.CaptionsEnabled,
                request.PlayerTheme ?? video.PlayerTheme);
        }

        if (request.TagIds is not null)
        {
            await ReplaceTagsAsync(video.Id, request.TagIds, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedVideo = await _unitOfWork.Videos.GetWithDetailsAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        return ContentMapper.ToDetail(updatedVideo);
    }

    private async Task ReplaceTagsAsync(Guid videoId, IReadOnlyCollection<Guid> requestedTagIds, CancellationToken cancellationToken)
    {
        var tagIds = requestedTagIds
            .Where(tagId => tagId != Guid.Empty)
            .Distinct()
            .ToArray();

        foreach (var tagId in tagIds)
        {
            if (!await _unitOfWork.Tags.ExistsAsync(tagId, cancellationToken))
            {
                throw new EntityNotFoundException("Tag", tagId);
            }
        }

        var existing = await _unitOfWork.VideoTags.GetByVideoIdAsync(videoId, cancellationToken);
        foreach (var videoTag in existing)
        {
            videoTag.Tag.DecrementUsageCount();
        }

        await _unitOfWork.VideoTags.DeleteByVideoIdAsync(videoId, cancellationToken);

        foreach (var tagId in tagIds)
        {
            var tag = await _unitOfWork.Tags.GetByIdAsync(tagId, cancellationToken)
                ?? throw new EntityNotFoundException("Tag", tagId);
            tag.IncrementUsageCount();
            await _unitOfWork.VideoTags.AddAsync(VideoTag.Create(videoId, tagId), cancellationToken);
        }
    }
}

public sealed class ArchiveVideoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public ArchiveVideoService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task Handle(Guid videoId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (!await _authorizationService.CanManageVideoAsync(videoId, userId, _currentUserService.Role, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have permission to manage this video");
        }

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        video.MarkAsDeleted();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetVideoProcessingStatusService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ReconcileVideoProcessingOrphansService _reconciler;

    public GetVideoProcessingStatusService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        ReconcileVideoProcessingOrphansService reconciler)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _reconciler = reconciler;
    }

    public async Task<VideoProcessingStatusDetailsDto> Handle(Guid videoId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (!await _authorizationService.CanManageVideoAsync(videoId, userId, _currentUserService.Role, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have permission to manage this video");
        }

        var video = await _unitOfWork.Videos.GetByIdAsync(videoId, cancellationToken)
            ?? throw new EntityNotFoundException("Video", videoId);
        var job = await _unitOfWork.VideoProcessingJobs.GetLatestByVideoIdAsync(videoId, cancellationToken);
        if (job is not null)
        {
            await _reconciler.Handle(job, cancellationToken);
        }

        return new VideoProcessingStatusDetailsDto(
            video.Id,
            video.Status,
            job?.Id,
            job?.JobType.ToString(),
            job?.Status.ToString(),
            job?.Progress,
            job?.ErrorMessage,
            job?.StartedAt,
            job?.CompletedAt);
    }
}

public sealed class ListCategoriesService
{
    private readonly IUnitOfWork _unitOfWork;

    public ListCategoriesService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CategoryDto>> Handle(CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        return categories
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(ContentMapper.ToCategory)
            .ToArray();
    }
}

public sealed class GetCategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto> Handle(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new EntityNotFoundException("Category", categoryId);
        return ContentMapper.ToCategory(category);
    }
}

public sealed class CreateCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateCategoryService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CategoryDto> Handle(CreateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        ContentGuards.EnsureAdmin(_currentUserService);

        if (await _unitOfWork.Categories.NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("A category with this name already exists");
        }

        if (request.ParentCategoryId.HasValue)
        {
            if (!await _unitOfWork.Categories.ExistsAsync(request.ParentCategoryId.Value, cancellationToken))
            {
                throw new EntityNotFoundException("Category", request.ParentCategoryId.Value);
            }
        }

        var category = Category.Create(
            request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.ParentCategoryId,
            request.DisplayOrder);

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ContentMapper.ToCategory(category);
    }
}

public sealed class UpdateCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCategoryService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CategoryDto> Handle(Guid categoryId, UpdateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        ContentGuards.EnsureAdmin(_currentUserService);

        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new EntityNotFoundException("Category", categoryId);

        var updatedName = request.Name is not null ? request.Name.Trim() : category.Name;
        var updatedDescription = request.ClearDescription
            ? null
            : request.Description is not null
                ? string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
                : category.Description;
        var updatedDisplayOrder = request.DisplayOrder ?? category.DisplayOrder;
        var updatedParentCategoryId = request.ClearParentCategory
            ? null
            : request.ParentCategoryId.HasValue
                ? request.ParentCategoryId.Value
                : category.ParentCategoryId;

        if (await _unitOfWork.Categories.NameExistsAsync(updatedName, categoryId, cancellationToken))
        {
            throw new InvalidOperationException("A category with this name already exists");
        }

        if (updatedParentCategoryId.HasValue)
        {
            if (updatedParentCategoryId.Value == categoryId)
            {
                throw new InvalidOperationException("Category cannot be its own parent");
            }

            if (!await _unitOfWork.Categories.ExistsAsync(updatedParentCategoryId.Value, cancellationToken))
            {
                throw new EntityNotFoundException("Category", updatedParentCategoryId.Value);
            }
        }

        category.Update(updatedName, updatedDescription, updatedDisplayOrder);
        category.SetParent(updatedParentCategoryId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ContentMapper.ToCategory(category);
    }
}

public sealed class DeleteCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCategoryService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid categoryId, CancellationToken cancellationToken)
    {
        ContentGuards.EnsureAdmin(_currentUserService);

        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new EntityNotFoundException("Category", categoryId);

        if (await _unitOfWork.Categories.HasSubcategoriesAsync(categoryId, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a category that still has subcategories");
        }

        if (await _unitOfWork.Categories.HasVideosAsync(categoryId, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a category that is assigned to videos");
        }

        await _unitOfWork.Categories.DeleteAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListTagsService
{
    private readonly IUnitOfWork _unitOfWork;

    public ListTagsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponseDto<TagSummaryDto>> Handle(ListTagsQuery query, CancellationToken cancellationToken)
    {
        var page = Pagination.NormalizePage(query.Page);
        var pageSize = Pagination.NormalizePageSize(query.PageSize);
        var result = await _unitOfWork.Tags.SearchPagedAsync(query.Search, page, pageSize, cancellationToken);
        return Pagination.Map(result, ContentMapper.ToTag);
    }
}

public sealed class GetTagService
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTagService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TagSummaryDto> Handle(Guid tagId, CancellationToken cancellationToken)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(tagId, cancellationToken)
            ?? throw new EntityNotFoundException("Tag", tagId);
        return ContentMapper.ToTag(tag);
    }
}

public sealed class CreateTagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateTagService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<TagSummaryDto> Handle(CreateTagRequestDto request, CancellationToken cancellationToken)
    {
        ContentGuards.EnsureAdmin(_currentUserService);

        if (await _unitOfWork.Tags.NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("A tag with this name already exists");
        }

        var tag = Tag.Create(request.Name);
        await _unitOfWork.Tags.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ContentMapper.ToTag(tag);
    }
}

public sealed class UpdateTagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTagService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<TagSummaryDto> Handle(Guid tagId, UpdateTagRequestDto request, CancellationToken cancellationToken)
    {
        ContentGuards.EnsureAdmin(_currentUserService);

        var tag = await _unitOfWork.Tags.GetByIdAsync(tagId, cancellationToken)
            ?? throw new EntityNotFoundException("Tag", tagId);

        var updatedName = request.Name is not null ? request.Name.Trim() : tag.Name;
        if (await _unitOfWork.Tags.NameExistsAsync(updatedName, tagId, cancellationToken))
        {
            throw new InvalidOperationException("A tag with this name already exists");
        }

        tag.UpdateName(updatedName);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ContentMapper.ToTag(tag);
    }
}

public sealed class DeleteTagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteTagService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid tagId, CancellationToken cancellationToken)
    {
        ContentGuards.EnsureAdmin(_currentUserService);

        var tag = await _unitOfWork.Tags.GetByIdAsync(tagId, cancellationToken)
            ?? throw new EntityNotFoundException("Tag", tagId);

        if (tag.UsageCount > 0 || await _unitOfWork.Tags.IsInUseAsync(tagId, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a tag that is assigned to videos");
        }

        await _unitOfWork.Tags.DeleteAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetUserProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUserProfileService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileDto> Handle(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException("User", userId);
        return ContentMapper.ToUserProfile(user, includePrivateFields: _currentUserService.Role == UserRole.Admin || _currentUserService.UserId == userId);
    }
}

public sealed class ListUsersService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListUsersService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponseDto<UserProfileDto>> Handle(ListUsersQuery query, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != UserRole.Admin)
        {
            throw new System.UnauthorizedAccessException("Only administrators can search users");
        }

        var page = Pagination.NormalizePage(query.Page);
        var pageSize = Pagination.NormalizePageSize(query.PageSize);
        var result = await _unitOfWork.Users.SearchPagedAsync(
            query.Search,
            query.Role,
            query.IsActive,
            page,
            pageSize,
            query.CreatedFrom,
            query.CreatedTo,
            cancellationToken);
        return Pagination.Map(result, user => ContentMapper.ToUserProfile(user, includePrivateFields: true));
    }
}

public sealed class ListMyUploadSessionsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListMyUploadSessionsService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponseDto<UploadSessionSummaryDto>> Handle(ListMyUploadSessionsQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        var page = Pagination.NormalizePage(query.Page);
        var pageSize = Pagination.NormalizePageSize(query.PageSize);
        var result = await _unitOfWork.UploadSessions.GetByUserIdPagedAsync(userId, query.Status, page, pageSize, cancellationToken);
        return Pagination.Map(result, ContentMapper.ToUploadSession);
    }
}

public sealed class ListVideoAccessGrantsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public ListVideoAccessGrantsService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<PagedResponseDto<AccessGrantDto>> Handle(ListVideoAccessGrantsQuery query, CancellationToken cancellationToken)
    {
        await EnsureCanManageAsync(query.VideoId, cancellationToken);
        var page = Pagination.NormalizePage(query.Page);
        var pageSize = Pagination.NormalizePageSize(query.PageSize);
        var result = await _unitOfWork.AccessControls.GetByVideoIdPagedAsync(query.VideoId, query.IsActive, page, pageSize, cancellationToken);
        return Pagination.Map(result, ContentMapper.ToAccessGrant);
    }

    private async Task EnsureCanManageAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (!await _authorizationService.CanManageVideoAsync(videoId, userId, _currentUserService.Role, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have permission to manage this video");
        }
    }
}

public sealed class CreateVideoAccessGrantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public CreateVideoAccessGrantService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<AccessGrantDto> Handle(Guid videoId, CreateAccessGrantRequestDto request, CancellationToken cancellationToken)
    {
        await EnsureCanManageAsync(videoId, cancellationToken);

        if (!await _unitOfWork.Videos.ExistsAsync(videoId, cancellationToken))
        {
            throw new EntityNotFoundException("Video", videoId);
        }

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration must be in the future", nameof(request.ExpiresAt));
        }

        var hasUser = request.UserId.HasValue;
        var hasToken = !string.IsNullOrWhiteSpace(request.ShareToken);
        if (hasUser && hasToken)
        {
            throw new ArgumentException("Specify either UserId or ShareToken, not both");
        }

        AccessControl accessControl;
        if (hasUser)
        {
            if (!await _unitOfWork.Users.ExistsAsync(request.UserId!.Value, cancellationToken))
            {
                throw new EntityNotFoundException("User", request.UserId.Value);
            }

            accessControl = AccessControl.CreateForUser(videoId, request.UserId.Value, request.PermissionType, request.ExpiresAt);
        }
        else
        {
            var shareToken = hasToken ? request.ShareToken!.Trim() : CreateShareToken();
            accessControl = AccessControl.CreateWithToken(videoId, shareToken, request.PermissionType, request.ExpiresAt);
        }

        await _unitOfWork.AccessControls.AddAsync(accessControl, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var reloaded = await _unitOfWork.AccessControls.GetByIdWithUserAsync(accessControl.Id, cancellationToken);
        return ContentMapper.ToAccessGrant(reloaded ?? accessControl);
    }

    private async Task EnsureCanManageAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (!await _authorizationService.CanManageVideoAsync(videoId, userId, _currentUserService.Role, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have permission to manage this video");
        }
    }

    private static string CreateShareToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

public sealed class RevokeVideoAccessGrantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public RevokeVideoAccessGrantService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task Handle(Guid videoId, Guid accessControlId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new System.UnauthorizedAccessException("User must be authenticated");
        if (!await _authorizationService.CanManageVideoAsync(videoId, userId, _currentUserService.Role, cancellationToken))
        {
            throw new System.UnauthorizedAccessException("You do not have permission to manage this video");
        }

        var accessControl = await _unitOfWork.AccessControls.GetByIdAsync(accessControlId, cancellationToken)
            ?? throw new EntityNotFoundException("AccessControl", accessControlId);
        if (accessControl.VideoId != videoId)
        {
            throw new EntityNotFoundException("AccessControl", accessControlId);
        }

        accessControl.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

internal static class Pagination
{
    private const int _defaultPage = 1;
    private const int _defaultPageSize = 24;
    private const int _maxPageSize = 100;

    public static int NormalizePage(int page) => page <= 0 ? _defaultPage : page;

    public static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return _defaultPageSize;
        }

        return Math.Min(pageSize, _maxPageSize);
    }

    public static PagedResponseDto<TDestination> Map<TSource, TDestination>(
        PagedQueryResult<TSource> result,
        Func<TSource, TDestination> map)
    {
        var totalPages = result.PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)result.TotalCount / result.PageSize);

        return new PagedResponseDto<TDestination>(
            result.Items.Select(map).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            totalPages,
            result.Page < totalPages,
            result.Page > 1 && totalPages > 0);
    }
}

internal static class ContentGuards
{
    public static void EnsureAdmin(ICurrentUserService currentUserService)
    {
        if (currentUserService.Role != UserRole.Admin)
        {
            throw new System.UnauthorizedAccessException("Only administrators can perform this action");
        }
    }
}

internal static class ContentMapper
{
    public static VideoSummaryDto ToSummary(Video video)
    {
        return new VideoSummaryDto(
            video.Id,
            video.Title,
            video.Description,
            video.UploaderId,
            video.Uploader?.Name ?? string.Empty,
            video.CategoryId,
            video.Category?.Name,
            video.Visibility,
            video.Status,
            ResolveDurationSeconds(video),
            video.ViewCount,
            video.CreatedAt,
            video.UpdatedAt,
            $"/api/v1/videos/{video.Id}/thumbnail",
            $"/api/v1/videos/{video.Id}/playback/manifest",
            video.VideoTags.Select(videoTag => ToTag(videoTag.Tag)).OrderBy(tag => tag.Name).ToArray());
    }

    private static int? ResolveDurationSeconds(Video video)
    {
        var duration = video.VideoVersions
            .Select(version => version.DurationSeconds)
            .Where(durationSeconds => durationSeconds > 0)
            .DefaultIfEmpty()
            .Max();

        return duration > 0 ? duration : null;
    }

    public static VideoDetailDto ToDetail(Video video)
    {
        return new VideoDetailDto(
            video.Id,
            video.Title,
            video.Description,
            video.UploaderId,
            video.Uploader?.Name ?? string.Empty,
            video.CategoryId,
            video.Category?.Name,
            video.Visibility,
            video.Status,
            video.AllowComments,
            video.AllowLikes,
            video.Autoplay,
            video.Loop,
            video.DefaultVolume,
            video.CaptionsEnabled,
            video.PlayerTheme,
            video.ViewCount,
            video.CreatedAt,
            video.UpdatedAt,
            $"/api/v1/videos/{video.Id}/thumbnail",
            $"/api/v1/videos/{video.Id}/playback/manifest",
            video.VideoTags.Select(videoTag => ToTag(videoTag.Tag)).OrderBy(tag => tag.Name).ToArray());
    }

    public static CategoryDto ToCategory(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.DisplayOrder,
            category.CreatedAt);
    }

    public static TagSummaryDto ToTag(Tag tag)
    {
        return new TagSummaryDto(tag.Id, tag.Name, tag.UsageCount);
    }

    public static UserProfileDto ToUserProfile(User user, bool includePrivateFields)
    {
        return new UserProfileDto(
            user.Id,
            user.Name,
            includePrivateFields ? user.Email : null,
            includePrivateFields ? user.Role : null,
            includePrivateFields ? user.IsActive : null,
            user.CreatedAt);
    }

    public static UploadSessionSummaryDto ToUploadSession(UploadSession session)
    {
        return new UploadSessionSummaryDto(
            session.Id,
            session.VideoId,
            session.Video?.Title,
            session.Status.ToString(),
            session.TotalSize,
            session.UploadedSize,
            session.GetProgress(),
            session.StorageProviderType.ToString(),
            session.ContentType,
            session.ExpiresAt,
            session.CreatedAt,
            session.UpdatedAt);
    }

    public static AccessGrantDto ToAccessGrant(AccessControl accessControl)
    {
        return new AccessGrantDto(
            accessControl.Id,
            accessControl.VideoId,
            accessControl.UserId,
            accessControl.User?.Name,
            accessControl.ShareToken,
            accessControl.PermissionType,
            accessControl.ExpiresAt,
            accessControl.IsActive,
            accessControl.CreatedAt);
    }
}
