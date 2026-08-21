using Microsoft.EntityFrameworkCore;
using StreamForge.Application.DTOs.Auth;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly StreamForgeDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;

    public AuthenticationService(
        StreamForgeDbContext dbContext,
        ITokenService tokenService,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var email = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new ValidationException("Password must be at least 8 characters long.");
        }

        if (await _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new DuplicateEntityException("User", "Email", email);
        }

        var passwordHash = PasswordHasher.Hash(request.Password);
        var user = User.Create(name, email, passwordHash, UserRole.Viewer);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new StreamForge.Domain.Exceptions.UnauthorizedAccessException("Invalid email or password.");
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.ValidateRefreshToken(request.RefreshToken)
            ?? throw new StreamForge.Domain.Exceptions.UnauthorizedAccessException("Invalid refresh token.");

        var userIdValue = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new StreamForge.Domain.Exceptions.UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new StreamForge.Domain.Exceptions.UnauthorizedAccessException("Invalid refresh token.");
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new StreamForge.Domain.Exceptions.UnauthorizedAccessException("The current user could not be resolved.");
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == _currentUserService.UserId.Value, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(_currentUserService.UserId.Value);
        }

        return MapUser(user);
    }

    private AuthResponseDto CreateAuthResponse(User user)
    {
        var tokens = _tokenService.CreateTokens(user);

        return new AuthResponseDto(
            MapUser(user),
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt);
    }

    private static AuthUserDto MapUser(User user)
    {
        return new AuthUserDto(user.Id, user.Name, user.Email, user.Role);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email cannot be empty.");
        }

        return email.Trim().ToLowerInvariant();
    }

}
