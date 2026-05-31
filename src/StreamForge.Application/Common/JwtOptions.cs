using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT SigningKey is required")]
    [StringLength(int.MaxValue, MinimumLength = 32, ErrorMessage = "SigningKey must be at least 32 characters")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "AccessTokenMinutes must be between 1 and 1440")]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 36500, ErrorMessage = "RefreshTokenDays must be between 1 and 36500")]
    public int RefreshTokenDays { get; set; } = 30;
}