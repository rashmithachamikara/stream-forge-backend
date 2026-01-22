namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines transcription processing status
/// </summary>
public enum TranscriptionStatus
{
    /// <summary>
    /// Transcription is queued
    /// </summary>
    Pending = 1,

    /// <summary>
    /// AI processing in progress
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Transcription completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Transcription failed
    /// </summary>
    Failed = 4
}
