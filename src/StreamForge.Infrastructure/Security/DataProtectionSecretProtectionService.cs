using Microsoft.AspNetCore.DataProtection;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.Security;

public sealed class DataProtectionSecretProtectionService : ISecretProtectionService
{
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtectionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("StreamForge.AdminSettings.Secrets.V1");
    }

    public string Protect(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new ArgumentException("Secret plaintext cannot be empty.", nameof(plaintext));
        }

        return _protector.Protect(plaintext.Trim());
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            throw new ArgumentException("Protected secret value cannot be empty.", nameof(protectedValue));
        }

        return _protector.Unprotect(protectedValue.Trim());
    }
}
