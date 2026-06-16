using StreamForge.Domain.Enums;

namespace StreamForge.Application.DTOs.Content;

public sealed record PagedResponseDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

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

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int DisplayOrder,
    DateTime CreatedAt);

public sealed record CreateCategoryRequestDto(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int DisplayOrder);

public sealed record UpdateCategoryRequestDto(
    string? Name,
    string? Description,
    Guid? ParentCategoryId,
    bool ClearParentCategory,
    int? DisplayOrder,
    bool ClearDescription);

public sealed record TagSummaryDto(
    Guid Id,
    string Name,
    int UsageCount);

public sealed record CreateTagRequestDto(string Name);

public sealed record UpdateTagRequestDto(string? Name);

public sealed record UserProfileDto(
    Guid Id,
    string Name,
    string? Email,
    UserRole? Role,
    bool? IsActive,
    DateTime CreatedAt);

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

public sealed record CreateAccessGrantRequestDto(
    Guid? UserId,
    string? ShareToken,
    PermissionType PermissionType,
    DateTime? ExpiresAt);
