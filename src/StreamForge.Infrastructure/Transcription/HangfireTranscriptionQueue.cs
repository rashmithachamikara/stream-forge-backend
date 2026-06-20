using Hangfire;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Infrastructure.Transcription;

public sealed class HangfireTranscriptionQueue : ITranscriptionQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireTranscriptionQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task EnqueueAsync(Guid videoId, string? language = null, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Enqueue<StartVideoTranscriptionService>(
            service => service.Handle(videoId, language, null, CancellationToken.None));
        return Task.CompletedTask;
    }
}
