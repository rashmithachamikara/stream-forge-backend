namespace StreamForge.Application.DTOs.Uploads;

/// <summary>
/// Request to create an upload session
/// </summary>
public sealed record CreateUploadSessionRequestDto(
    string Title,
    string? Description,
    long TotalSize,
    string? ContentType,
    Guid? CategoryId = null);

/// <summary>
/// Response after creating an upload session
/// </summary>
public sealed record CreateUploadSessionResponseDto(
    Guid SessionId,
    DateTime ExpiresAt,
    string VideoTitle);

/// <summary>
/// Target where client should upload
/// </summary>
public sealed record UploadTargetDto(
    string Type,
    string Url,
    Dictionary<string, string>? Headers = null,
    string? HttpMethod = null);

/// <summary>
/// Request to upload a part
/// </summary>
public sealed record UploadPartRequestDto(
    Guid SessionId,
    int PartNumber,
    long PartSize,
    string Checksum);

/// <summary>
/// Response after uploading a part
/// </summary>
public sealed record UploadPartResponseDto(
    Guid SessionId,
    int PartNumber,
    bool IsComplete);

/// <summary>
/// Request to complete an upload session
/// </summary>
public sealed record CompleteUploadSessionRequestDto(
    Guid SessionId,
    string FileName);

/// <summary>
/// Response after completing upload
/// </summary>
public sealed record CompleteUploadSessionResponseDto(
    Guid SessionId,
    Guid? VideoId,
    string Status);

/// <summary>
/// Get upload target request
/// </summary>
public sealed record GetUploadTargetRequestDto(
    Guid SessionId,
    int PartNumber,
    long PartSize);
