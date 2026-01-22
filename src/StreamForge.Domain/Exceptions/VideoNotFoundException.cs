namespace StreamForge.Domain.Exceptions;

/// <summary>
/// Exception thrown when a video is not found
/// </summary>
public class VideoNotFoundException : EntityNotFoundException
{
    public VideoNotFoundException(Guid videoId)
        : base("Video", videoId)
    {
    }
}
