using StreamForge.Application.DTOs.Auth;

namespace StreamForge.Application.Interfaces;

/// <summary>
/// Handles authentication flows for registration, login, token refresh, and current-user resolution.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user account and returns the authenticated session payload.
    /// </summary>
    /// <param name="request">Registration details for the new user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user and issued tokens.</returns>
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates an existing user and returns issued access and refresh tokens.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user and issued tokens.</returns>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a valid refresh token for a new authenticated session payload.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user and newly issued tokens.</returns>
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the authenticated user represented by the current request context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current authenticated user.</returns>
    Task<AuthUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
