using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Infrastructure.Security;

public sealed class SystemSecretStoreService : ISystemSecretStoreService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecretProtectionService _secretProtectionService;

    public SystemSecretStoreService(
        IUnitOfWork unitOfWork,
        ISecretProtectionService secretProtectionService)
    {
        _unitOfWork = unitOfWork;
        _secretProtectionService = secretProtectionService;
    }

    public async Task<string?> ResolveAsync(
        string key,
        string? fallbackValue,
        CancellationToken cancellationToken = default)
    {
        var secret = await _unitOfWork.SystemSecrets.GetByKeyAsync(key, cancellationToken);
        if (secret is not null)
        {
            return _secretProtectionService.Unprotect(secret.EncryptedValue);
        }

        return string.IsNullOrWhiteSpace(fallbackValue) ? null : fallbackValue.Trim();
    }

    public async Task<SecretConfigurationStatus> GetStatusAsync(
        string key,
        string? fallbackValue,
        CancellationToken cancellationToken = default)
    {
        var value = await ResolveAsync(key, fallbackValue, cancellationToken);
        return string.IsNullOrWhiteSpace(value)
            ? new SecretConfigurationStatus(false, null)
            : new SecretConfigurationStatus(true, MaskValue(value));
    }

    public async Task SetAsync(string key, string plaintextValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plaintextValue))
        {
            throw new ArgumentException("Secret plaintext value cannot be empty.", nameof(plaintextValue));
        }

        var encryptedValue = _secretProtectionService.Protect(plaintextValue);
        var existing = await _unitOfWork.SystemSecrets.GetByKeyAsync(key, cancellationToken);

        if (existing is null)
        {
            await _unitOfWork.SystemSecrets.AddAsync(SystemSecret.Create(key, encryptedValue), cancellationToken);
            return;
        }

        existing.SetEncryptedValue(encryptedValue);
        await _unitOfWork.SystemSecrets.UpdateAsync(existing, cancellationToken);
    }

    public async Task ClearAsync(string key, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.SystemSecrets.GetByKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            await _unitOfWork.SystemSecrets.DeleteAsync(existing, cancellationToken);
        }
    }

    private static string MaskValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
        {
            return "****";
        }

        return $"****{trimmed[^4..]}";
    }
}
