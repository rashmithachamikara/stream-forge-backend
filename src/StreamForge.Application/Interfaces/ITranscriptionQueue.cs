namespace StreamForge.Application.Interfaces;

public interface ITranscriptionQueue
{
    Task EnqueueAsync(Guid videoId, string? language = null, CancellationToken cancellationToken = default);
}
