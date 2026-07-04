namespace StreamForge.Application.Interfaces;

public sealed record SecretConfigurationStatus(
    bool IsConfigured,
    string? MaskedValue);

public interface ISecretProtectionService
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);
}

public interface ISystemSecretStoreService
{
    Task<string?> ResolveAsync(string key, string? fallbackValue, CancellationToken cancellationToken = default);

    Task<SecretConfigurationStatus> GetStatusAsync(string key, string? fallbackValue, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string plaintextValue, CancellationToken cancellationToken = default);

    Task ClearAsync(string key, CancellationToken cancellationToken = default);
}
