using StreamForge.Domain.Enums;

namespace StreamForge.Application.DTOs.Content;

/// <summary>
/// Generic paged response wrapper used by list and search endpoints.
/// </summary>
public sealed record PagedResponseDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

/// <summary>
/// Summary representation of a video item in listing responses.
/// </summary>
public sealed record VideoSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    Guid UploaderId,
    string UploaderName,
    Guid? CategoryId,
    string? CategoryName,
    VideoVisibility Visibility,
    VideoStatus Status,
    int? DurationSeconds,
    long ViewCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ThumbnailUrl,
    string PlaybackManifestUrl,
    IReadOnlyList<TagSummaryDto> Tags);

/// <summary>
/// Detailed representation of a single video.
/// </summary>
public sealed record VideoDetailDto(
    Guid Id,
    string Title,
    string? Description,
    Guid UploaderId,
    string UploaderName,
    Guid? CategoryId,
    string? CategoryName,
    VideoVisibility Visibility,
    VideoStatus Status,
    bool AllowComments,
    bool AllowLikes,
    bool Autoplay,
    bool Loop,
    int DefaultVolume,
    bool CaptionsEnabled,
    string PlayerTheme,
    long ViewCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ThumbnailUrl,
    string PlaybackManifestUrl,
    IReadOnlyList<TagSummaryDto> Tags);

/// <summary>
/// Partial update payload for editable video metadata and player settings.
/// </summary>
public sealed record UpdateVideoRequestDto(
    string? Title,
    string? Description,
    Guid? CategoryId,
    VideoVisibility? Visibility,
    IReadOnlyCollection<Guid>? TagIds,
    bool? AllowComments,
    bool? AllowLikes,
    bool? Autoplay,
    bool? Loop,
    int? DefaultVolume,
    bool? CaptionsEnabled,
    string? PlayerTheme);

/// <summary>
/// Current processing-state payload for a video.
/// </summary>
public sealed record VideoProcessingStatusDetailsDto(
    Guid VideoId,
    VideoStatus VideoStatus,
    Guid? ProcessingJobId,
    string? JobType,
    string? JobStatus,
    int? Progress,
    string? ErrorMessage,
    DateTime? StartedAt,
    DateTime? CompletedAt);

/// <summary>
/// Category payload returned by category endpoints.
/// </summary>
public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int DisplayOrder,
    DateTime CreatedAt);

/// <summary>
/// Request payload for creating a category.
/// </summary>
public sealed record CreateCategoryRequestDto(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int DisplayOrder);

/// <summary>
/// Request payload for updating an existing category.
/// </summary>
public sealed record UpdateCategoryRequestDto(
    string? Name,
    string? Description,
    Guid? ParentCategoryId,
    bool ClearParentCategory,
    int? DisplayOrder,
    bool ClearDescription);

/// <summary>
/// Tag summary payload returned in listings and video metadata.
/// </summary>
public sealed record TagSummaryDto(
    Guid Id,
    string Name,
    int UsageCount);

/// <summary>
/// Request payload for creating a tag.
/// </summary>
public sealed record CreateTagRequestDto(string Name);

/// <summary>
/// Request payload for renaming a tag.
/// </summary>
public sealed record UpdateTagRequestDto(string? Name);

/// <summary>
/// User profile payload returned by user and profile endpoints.
/// </summary>
public sealed record UserProfileDto(
    Guid Id,
    string Name,
    string? Email,
    UserRole? Role,
    bool? IsActive,
    DateTime CreatedAt);

/// <summary>
/// Summary representation of an upload session.
/// </summary>
public sealed record UploadSessionSummaryDto(
    Guid Id,
    Guid VideoId,
    string? VideoTitle,
    string Status,
    long TotalSize,
    long UploadedSize,
    decimal Progress,
    string StorageProviderType,
    string? ContentType,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Video access grant payload returned by access-management endpoints.
/// </summary>
public sealed record AccessGrantDto(
    Guid Id,
    Guid VideoId,
    Guid? UserId,
    string? UserName,
    string? ShareToken,
    PermissionType PermissionType,
    DateTime? ExpiresAt,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>
/// Request payload for creating a video access grant.
/// </summary>
public sealed record CreateAccessGrantRequestDto(
    Guid? UserId,
    string? ShareToken,
    PermissionType PermissionType,
    DateTime? ExpiresAt);
