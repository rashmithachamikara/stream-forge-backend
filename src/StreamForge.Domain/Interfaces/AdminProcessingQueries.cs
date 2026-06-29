using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

public sealed record AdminVideoProcessingJobsQuery(
    int Page,
    int PageSize,
    ProcessingJobStatus? Status,
    Guid? VideoId,
    Guid? UploaderUserId,
    string? Search,
    DateTime? CreatedFrom,
    DateTime? CreatedTo,
    DateTime? StartedFrom,
    DateTime? StartedTo,
    DateTime? CompletedFrom,
    DateTime? CompletedTo,
    bool? HasError,
    string SortBy,
    bool SortDescending);

public sealed record AdminTranscriptionJobsQuery(
    int Page,
    int PageSize,
    TranscriptionStatus? Status,
    Guid? VideoId,
    Guid? UploaderUserId,
    string? Search,
    DateTime? CreatedFrom,
    DateTime? CreatedTo,
    bool? HasError,
    string? Provider,
    string? Language,
    string? Format,
    string? Source,
    string SortBy,
    bool SortDescending);
