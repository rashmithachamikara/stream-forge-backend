namespace StreamForge.Domain.Entities;

/// <summary>
/// Searchable transcript chunk derived from transcription segment data.
/// </summary>
public sealed class VideoTranscriptChunk : BaseEntity
{
    public Guid VideoId { get; private set; }

    public Guid TranscriptionId { get; private set; }

    public string Language { get; private set; }

    public double StartSeconds { get; private set; }

    public double EndSeconds { get; private set; }

    public string Content { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public Video Video { get; private set; } = null!;

    public VideoTranscription Transcription { get; private set; } = null!;

    private VideoTranscriptChunk() : base()
    {
        Language = string.Empty;
        Content = string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public static VideoTranscriptChunk Create(
        Guid videoId,
        Guid transcriptionId,
        string language,
        double startSeconds,
        double endSeconds,
        string content)
    {
        if (videoId == Guid.Empty)
        {
            throw new ArgumentException("Video ID is required.", nameof(videoId));
        }

        if (transcriptionId == Guid.Empty)
        {
            throw new ArgumentException("Transcription ID is required.", nameof(transcriptionId));
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("Language is required.", nameof(language));
        }

        if (startSeconds < 0)
        {
            throw new ArgumentException("Start seconds cannot be negative.", nameof(startSeconds));
        }

        if (endSeconds < startSeconds)
        {
            throw new ArgumentException("End seconds must be greater than or equal to start seconds.", nameof(endSeconds));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Chunk content is required.", nameof(content));
        }

        return new VideoTranscriptChunk
        {
            VideoId = videoId,
            TranscriptionId = transcriptionId,
            Language = language.Trim().ToLowerInvariant(),
            StartSeconds = startSeconds,
            EndSeconds = endSeconds,
            Content = content.Trim(),
            UpdatedAt = DateTime.UtcNow
        };
    }
}
