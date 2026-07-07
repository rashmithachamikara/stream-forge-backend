namespace StreamForge.Application.DTOs.Processing;

/// <summary>
/// Query parameters for the admin video-processing jobs endpoint.
/// </summary>
public sealed class AdminVideoProcessingJobsQueryDto
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string? Status { get; init; }
    public Guid? VideoId { get; init; }
    public Guid? UploaderUserId { get; init; }
    public string? Search { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public DateTime? StartedFrom { get; init; }
    public DateTime? StartedTo { get; init; }
    public DateTime? CompletedFrom { get; init; }
    public DateTime? CompletedTo { get; init; }
    public bool? HasError { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}

/// <summary>
/// Lightweight processing status payload for a single video.
/// </summary>
public sealed record VideoProcessingStatusDto(
    Guid VideoId,
    Guid ProcessingJobId,
    string Status,
    int Progress,
    string? ErrorMessage);

/// <summary>
/// Administrative view of a video-processing job.
/// </summary>
public sealed record AdminVideoProcessingJobDto(
    string JobKey,
    Guid VideoId,
    string VideoTitle,
    string JobType,
    string Status,
    int Progress,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string VideoStatus);
