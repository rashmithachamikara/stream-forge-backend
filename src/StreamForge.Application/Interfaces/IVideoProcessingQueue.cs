namespace StreamForge.Application.Interfaces;

public interface IVideoProcessingQueue
{
    Task EnqueueAsync(Guid processingJobId, CancellationToken cancellationToken = default);
}
