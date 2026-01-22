namespace StreamForge.Domain.Enums;

/// <summary>
/// Defines user roles in the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// System administrator with full access
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Content editor who can upload and manage videos
    /// </summary>
    Editor = 2,

    /// <summary>
    /// Regular viewer who can watch and interact with videos
    /// </summary>
    Viewer = 3
}
