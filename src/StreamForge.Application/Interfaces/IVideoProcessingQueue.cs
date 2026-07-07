namespace StreamForge.Application.Interfaces;

/// <summary>
/// Enqueues background video-processing work.
/// </summary>
public interface IVideoProcessingQueue
{
    /// <summary>
    /// Queues a processing job for execution by the background runtime.
    /// </summary>
    /// <param name="processingJobId">Processing job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnqueueAsync(Guid processingJobId, CancellationToken cancellationToken = default);
}
