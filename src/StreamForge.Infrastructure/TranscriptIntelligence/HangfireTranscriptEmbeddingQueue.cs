using Hangfire;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.TranscriptIntelligence;

namespace StreamForge.Infrastructure.TranscriptIntelligence;

public sealed class HangfireTranscriptEmbeddingQueue : ITranscriptEmbeddingQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireTranscriptEmbeddingQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task EnqueueAsync(Guid videoId, string language, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Enqueue<GenerateTranscriptEmbeddingsService>(
            service => service.Handle(videoId, language, CancellationToken.None));
        return Task.CompletedTask;
    }
}
