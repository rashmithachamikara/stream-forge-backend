using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents an AI-generated video transcription
/// </summary>
public class VideoTranscription : BaseEntity
{
    /// <summary>
    /// Video ID
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// ISO language code (e.g., "en", "es", "fr")
    /// </summary>
    public string Language { get; private set; }

    /// <summary>
    /// Transcription format (SRT, VTT, TXT)
    /// </summary>
    public string Format { get; private set; }

    /// <summary>
    /// Storage path to transcription file
    /// </summary>
    public string StoragePath { get; private set; }

    /// <summary>
    /// Transcription status
    /// </summary>
    public TranscriptionStatus Status { get; private set; }

    /// <summary>
    /// AI provider source (e.g., "OpenAI", "Azure", "Manual")
    /// </summary>
    public string Source { get; private set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public Video Video { get; private set; } = null!;

    // Private constructor for EF Core
    private VideoTranscription() : base()
    {
        Language = string.Empty;
        Format = string.Empty;
        StoragePath = string.Empty;
        Source = string.Empty;
    }

    /// <summary>
    /// Creates a new transcription
    /// </summary>
    public static VideoTranscription Create(
        Guid videoId,
        string language,
        string format,
        string storagePath,
        string source)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be empty", nameof(language));

        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be empty", nameof(format));

        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty", nameof(storagePath));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        var transcription = new VideoTranscription
        {
            VideoId = videoId,
            Language = language.ToLowerInvariant(),
            Format = format.ToUpperInvariant(),
            StoragePath = storagePath,
            Status = TranscriptionStatus.Pending,
            Source = source
        };

        return transcription;
    }

    /// <summary>
    /// Updates transcription status to processing
    /// </summary>
    public void StartProcessing()
    {
        if (Status != TranscriptionStatus.Pending)
            throw new InvalidOperationException("Can only start processing from Pending status");

        Status = TranscriptionStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void QueueForProcessing(string storagePath, string source)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty", nameof(storagePath));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        StoragePath = storagePath;
        Source = source;
        Status = TranscriptionStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks transcription as completed
    /// </summary>
    public void Complete(string storagePath, string? language = null)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty", nameof(storagePath));

        if (Status != TranscriptionStatus.Processing)
            throw new InvalidOperationException("Can only complete from Processing status");

        StoragePath = storagePath;
        if (!string.IsNullOrWhiteSpace(language))
        {
            Language = language.ToLowerInvariant();
        }

        Status = TranscriptionStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks transcription as failed
    /// </summary>
    public void Fail()
    {
        Status = TranscriptionStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}
