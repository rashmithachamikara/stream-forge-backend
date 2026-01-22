using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a background video processing job
/// </summary>
public class VideoProcessingJob : BaseEntity
{
    /// <summary>
    /// Video ID being processed
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// Type of processing job
    /// </summary>
    public ProcessingJobType JobType { get; private set; }

    /// <summary>
    /// Job status
    /// </summary>
    public ProcessingJobStatus Status { get; private set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; private set; }

    /// <summary>
    /// Error message if job failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// When processing started
    /// </summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>
    /// When processing completed
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    // Navigation properties
    public Video Video { get; private set; } = null!;

    // Private constructor for EF Core
    private VideoProcessingJob() : base()
    {
    }

    /// <summary>
    /// Creates a new processing job
    /// </summary>
    public static VideoProcessingJob Create(Guid videoId, ProcessingJobType jobType)
    {
        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        var job = new VideoProcessingJob
        {
            VideoId = videoId,
            JobType = jobType,
            Status = ProcessingJobStatus.Pending,
            Progress = 0
        };

        return job;
    }

    /// <summary>
    /// Starts the job
    /// </summary>
    public void Start()
    {
        if (Status != ProcessingJobStatus.Pending)
            throw new InvalidOperationException("Job can only be started from Pending status");

        Status = ProcessingJobStatus.Processing;
        StartedAt = DateTime.UtcNow;
        Progress = 0;
    }

    /// <summary>
    /// Updates job progress
    /// </summary>
    public void UpdateProgress(int progress)
    {
        if (progress < 0 || progress > 100)
            throw new ArgumentException("Progress must be between 0 and 100", nameof(progress));

        if (Status != ProcessingJobStatus.Processing)
            throw new InvalidOperationException("Progress can only be updated for processing jobs");

        Progress = progress;
    }

    /// <summary>
    /// Marks job as completed
    /// </summary>
    public void Complete()
    {
        if (Status != ProcessingJobStatus.Processing)
            throw new InvalidOperationException("Job must be processing to complete");

        Status = ProcessingJobStatus.Completed;
        Progress = 100;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks job as failed
    /// </summary>
    public void Fail(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message is required", nameof(errorMessage));

        Status = ProcessingJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets job to pending for retry
    /// </summary>
    public void Reset()
    {
        Status = ProcessingJobStatus.Pending;
        Progress = 0;
        ErrorMessage = null;
        StartedAt = null;
        CompletedAt = null;
    }
}
