using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Auth;
using StreamForge.Application.Interfaces;

namespace StreamForge.Api.Controllers;

/// <summary>
/// Exposes authentication endpoints for registration, login, token refresh, and current-user lookup.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Registers a new user account and returns an authenticated session payload.
    /// </summary>
    /// <param name="request">Registration details for the new user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user and issued tokens.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authenticationService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates an existing user and returns access and refresh tokens.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user and issued tokens.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authenticationService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Exchanges a refresh token for a new authenticated session.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user and newly issued tokens.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authenticationService.RefreshTokenAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Gets the authenticated user represented by the current access token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current authenticated user profile.</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var currentUser = await _authenticationService.GetCurrentUserAsync(cancellationToken);
        return Ok(currentUser);
    }
}
