using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

public interface IVideoTranscriptionRepository : IRepository<VideoTranscription>
{
    Task<IReadOnlyList<VideoTranscription>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);
    Task<VideoTranscription?> GetWithVideoAsync(Guid transcriptionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscription>> GetByVideoAndStatusAsync(
        Guid videoId,
        params TranscriptionStatus[] statuses);

    Task<VideoTranscription?> GetByVideoLanguageAndFormatAsync(
        Guid videoId,
        string language,
        string format,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscription>> GetByWorkerJobIdAsync(
        string workerJobId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscription>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscription>> GetAllOrderedAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscription>> GetByStatusesAsync(
        CancellationToken cancellationToken = default,
        params TranscriptionStatus[] statuses);

    Task<IReadOnlyList<VideoTranscription>> QueryAdminRowsAsync(
        AdminTranscriptionJobsQuery query,
        CancellationToken cancellationToken = default);
}
