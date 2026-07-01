namespace StreamForge.Application.Interfaces;

public interface ITranscriptEmbeddingQueue
{
    Task EnqueueAsync(Guid videoId, string language, CancellationToken cancellationToken = default);
}
