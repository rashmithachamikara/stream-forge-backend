namespace StreamForge.Domain.Entities;

/// <summary>
/// Generic system-level key-value setting.
/// </summary>
public sealed class SystemSetting : BaseEntity
{
    public string Key { get; private set; }

    public string Value { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private SystemSetting() : base()
    {
        Key = string.Empty;
        Value = string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public static SystemSetting Create(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Setting key cannot be empty.", nameof(key));
        }

        var normalizedKey = key.Trim();
        return new SystemSetting
        {
            Key = normalizedKey,
            Value = value ?? string.Empty,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void SetValue(string? value)
    {
        Value = value ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
}
