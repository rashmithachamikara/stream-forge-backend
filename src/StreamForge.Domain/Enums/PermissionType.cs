namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines permission types for video access
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// Can view/watch the video
    /// </summary>
    View = 1,

    /// <summary>
    /// Can embed the video
    /// </summary>
    Embed = 2,

    /// <summary>
    /// Can download the video
    /// </summary>
    Download = 3
}
