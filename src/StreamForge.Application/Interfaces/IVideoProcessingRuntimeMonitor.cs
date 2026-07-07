namespace StreamForge.Application.Interfaces;

/// <summary>
/// Reports whether a video-processing job is still actively executing in the runtime.
/// </summary>
public interface IVideoProcessingRuntimeMonitor
{
    /// <summary>
    /// Determines whether the supplied processing job currently has an active execution.
    /// </summary>
    /// <param name="processingJobId">Processing job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the job is actively executing; otherwise <see langword="false"/>.</returns>
    Task<bool> HasActiveExecutionAsync(Guid processingJobId, CancellationToken cancellationToken = default);
}
