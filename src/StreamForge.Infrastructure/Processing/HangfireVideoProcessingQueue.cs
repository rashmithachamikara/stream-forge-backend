using Hangfire;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Processing;

namespace StreamForge.Infrastructure.Processing;

public sealed class HangfireVideoProcessingQueue : IVideoProcessingQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireVideoProcessingQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task EnqueueAsync(Guid processingJobId, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Enqueue<ProcessVideoJobService>(
            service => service.Handle(processingJobId, CancellationToken.None));
        return Task.CompletedTask;
    }
}
