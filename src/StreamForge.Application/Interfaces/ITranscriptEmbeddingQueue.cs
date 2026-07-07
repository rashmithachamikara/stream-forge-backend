namespace StreamForge.Application.Interfaces;

/// <summary>
/// Enqueues transcript embedding generation for background processing.
/// </summary>
public interface ITranscriptEmbeddingQueue
{
    /// <summary>
    /// Queues transcript embedding generation for a video and language scope.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="language">Transcript language to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnqueueAsync(Guid videoId, string language, CancellationToken cancellationToken = default);
}
