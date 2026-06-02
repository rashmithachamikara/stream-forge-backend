namespace StreamForge.Application.DTOs.Processing;

public sealed record VideoProcessingStatusDto(
    Guid VideoId,
    Guid ProcessingJobId,
    string Status,
    int Progress,
    string? ErrorMessage);
