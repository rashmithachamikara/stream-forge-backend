namespace StreamForge.Application.DTOs.Processing;

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
