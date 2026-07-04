namespace StreamForge.Domain.Entities;

/// <summary>
/// Encrypted system secret value.
/// </summary>
public sealed class SystemSecret : BaseEntity
{
    public string Key { get; private set; }

    public string EncryptedValue { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private SystemSecret() : base()
    {
        Key = string.Empty;
        EncryptedValue = string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public static SystemSecret Create(string key, string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Secret key cannot be empty.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            throw new ArgumentException("Encrypted value cannot be empty.", nameof(encryptedValue));
        }

        return new SystemSecret
        {
            Key = key.Trim(),
            EncryptedValue = encryptedValue.Trim(),
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void SetEncryptedValue(string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            throw new ArgumentException("Encrypted value cannot be empty.", nameof(encryptedValue));
        }

        EncryptedValue = encryptedValue.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
