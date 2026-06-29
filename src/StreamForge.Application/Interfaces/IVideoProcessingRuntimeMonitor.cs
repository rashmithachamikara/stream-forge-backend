namespace StreamForge.Application.Interfaces;

public interface IVideoProcessingRuntimeMonitor
{
    Task<bool> HasActiveExecutionAsync(Guid processingJobId, CancellationToken cancellationToken = default);
}
