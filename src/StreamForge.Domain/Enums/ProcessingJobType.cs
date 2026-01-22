namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines types of video processing jobs
/// </summary>
public enum ProcessingJobType
{
    /// <summary>
    /// Video transcoding to different resolutions/formats
    /// </summary>
    Transcode = 1,

    /// <summary>
    /// Thumbnail image generation
    /// </summary>
    Thumbnail = 2,

    /// <summary>
    /// AI-powered transcription
    /// </summary>
    Transcription = 3,

    /// <summary>
    /// Video content analysis
    /// </summary>
    Analysis = 4
}
