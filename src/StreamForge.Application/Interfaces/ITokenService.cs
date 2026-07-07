using System.Security.Claims;
using StreamForge.Application.DTOs.Auth;
using StreamForge.Domain.Entities;

namespace StreamForge.Application.Interfaces;

/// <summary>
/// Issues and validates authentication tokens used by the API.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates access and refresh tokens for the supplied user.
    /// </summary>
    /// <param name="user">Authenticated user.</param>
    /// <returns>Issued access and refresh token values with expiry metadata.</returns>
    AuthTokenResultDto CreateTokens(User user);

    /// <summary>
    /// Validates a refresh token and returns the resolved principal when valid.
    /// </summary>
    /// <param name="refreshToken">Refresh token value.</param>
    /// <returns>The principal represented by the token, or <see langword="null"/> when invalid.</returns>
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}
