namespace StreamForge.Application.DTOs.Processing;

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

public sealed record VideoProcessingStatusDto(
    Guid VideoId,
    Guid ProcessingJobId,
    string Status,
    int Progress,
    string? ErrorMessage);

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
