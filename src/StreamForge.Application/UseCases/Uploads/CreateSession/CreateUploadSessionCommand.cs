using StreamForge.Application.DTOs.Uploads;
using StreamForge.Domain.Enums;

namespace StreamForge.Application.UseCases.Uploads.CreateSession;

/// <summary>
/// DTO for creating a new upload session
/// </summary>
public record CreateUploadSessionRequest(
    string Title,
    string? Description,
    long TotalSize,
    string? ContentType,
    Guid? CategoryId = null,
    VideoVisibility? Visibility = null,
    IReadOnlyCollection<Guid>? TagIds = null);
