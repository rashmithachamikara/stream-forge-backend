namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines processing job status
/// </summary>
public enum ProcessingJobStatus
{
    /// <summary>
    /// Job is queued and waiting to be processed
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Job is currently being processed
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Job failed with errors
    /// </summary>
    Failed = 4
}
