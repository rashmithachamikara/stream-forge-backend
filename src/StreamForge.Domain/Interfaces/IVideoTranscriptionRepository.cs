using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Interfaces;

public interface IVideoTranscriptionRepository : IRepository<VideoTranscription>
{
    Task<IReadOnlyList<VideoTranscription>> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VideoTranscription>> GetByVideoAndStatusAsync(
        Guid videoId,
        params TranscriptionStatus[] statuses);

    Task<VideoTranscription?> GetByVideoLanguageAndFormatAsync(
        Guid videoId,
        string language,
        string format,
        CancellationToken cancellationToken = default);
}
