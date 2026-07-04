namespace StreamForge.Application.Common;

public sealed class DataProtectionOptions
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; set; } = "StreamForge";

    public string? KeysPath { get; set; }
}
