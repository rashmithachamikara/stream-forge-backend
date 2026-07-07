namespace StreamForge.Application.Interfaces;

/// <summary>
/// Indicates whether a secret is configured without exposing its plaintext value.
/// </summary>
public sealed record SecretConfigurationStatus(
    bool IsConfigured,
    string? MaskedValue);

/// <summary>
/// Protects and unprotects secret values before persistence and at resolution time.
/// </summary>
public interface ISecretProtectionService
{
    /// <summary>
    /// Encrypts or otherwise protects a plaintext secret value.
    /// </summary>
    /// <param name="plaintext">Plaintext secret value.</param>
    /// <returns>The protected representation suitable for storage.</returns>
    string Protect(string plaintext);

    /// <summary>
    /// Restores a protected secret value to plaintext.
    /// </summary>
    /// <param name="protectedValue">Protected secret value.</param>
    /// <returns>The decrypted plaintext value.</returns>
    string Unprotect(string protectedValue);
}

/// <summary>
/// Resolves, stores, and clears encrypted runtime system secrets.
/// </summary>
public interface ISystemSecretStoreService
{
    /// <summary>
    /// Resolves an effective secret value, falling back to a configured default when no stored secret exists.
    /// </summary>
    /// <param name="key">Stable secret key.</param>
    /// <param name="fallbackValue">Optional fallback value from configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective secret value, or <see langword="null"/> when none exists.</returns>
    Task<string?> ResolveAsync(string key, string? fallbackValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets non-sensitive status information for a secret.
    /// </summary>
    /// <param name="key">Stable secret key.</param>
    /// <param name="fallbackValue">Optional fallback value from configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Masked configuration status for the secret.</returns>
    Task<SecretConfigurationStatus> GetStatusAsync(string key, string? fallbackValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores or replaces a secret value.
    /// </summary>
    /// <param name="key">Stable secret key.</param>
    /// <param name="plaintextValue">Plaintext secret value to protect and persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync(string key, string plaintextValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a stored secret value for the supplied key.
    /// </summary>
    /// <param name="key">Stable secret key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAsync(string key, CancellationToken cancellationToken = default);
}
