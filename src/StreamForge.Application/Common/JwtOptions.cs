using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

/// <summary>
/// Configures JWT token issuance and validation behavior for the API.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the token issuer value.
    /// </summary>
    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token audience value.
    /// </summary>
    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symmetric signing key used for JWT creation and validation.
    /// </summary>
    [Required(ErrorMessage = "JWT SigningKey is required")]
    [StringLength(int.MaxValue, MinimumLength = 32, ErrorMessage = "SigningKey must be at least 32 characters")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access token lifetime in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "AccessTokenMinutes must be between 1 and 1440")]
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the refresh token lifetime in days.
    /// </summary>
    [Range(1, 36500, ErrorMessage = "RefreshTokenDays must be between 1 and 36500")]
    public int RefreshTokenDays { get; set; } = 30;
}
