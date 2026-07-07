namespace StreamForge.Application.Interfaces;

/// <summary>
/// Enqueues video transcription work for background processing.
/// </summary>
public interface ITranscriptionQueue
{
    /// <summary>
    /// Queues transcription for a video.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="language">Optional requested language override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnqueueAsync(Guid videoId, string? language = null, CancellationToken cancellationToken = default);
}
