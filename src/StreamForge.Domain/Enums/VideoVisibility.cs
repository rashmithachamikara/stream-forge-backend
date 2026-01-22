namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines video visibility settings
/// </summary>
public enum VideoVisibility
{
    /// <summary>
    /// Video is accessible to everyone
    /// </summary>
    Public = 1,

    /// <summary>
    /// Video is only accessible by the owner
    /// </summary>
    Private = 2,

    /// <summary>
    /// Video is accessible to authenticated users only
    /// </summary>
    Internal = 3
}
