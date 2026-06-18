namespace StreamForge.Application.Common;

/// <summary>
/// Local filesystem path conventions used across Stream Forge components.
/// </summary>
public class LocalStorageOptions
{
    /// <summary>
    /// Optional shared local root path override. When empty, a repo-root anchored default is used.
    /// </summary>
    public string? RootPath { get; set; }

    /// <summary>
    /// Relative uploads directory under the local storage root.
    /// </summary>
    public string UploadsRelativePath { get; set; } = "uploads";

    /// <summary>
    /// Relative transcription output directory under the local storage root.
    /// </summary>
    public string TranscriptionOutputRelativePath { get; set; } = "transcription-output";
}
