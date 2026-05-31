using System.ComponentModel.DataAnnotations;

namespace StreamForge.Application.Common;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(ErrorMessage = "DefaultConnection is required")]
    public string DefaultConnection { get; set; } = string.Empty;
}
