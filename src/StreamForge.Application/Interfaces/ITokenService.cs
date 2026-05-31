using System.Security.Claims;
using StreamForge.Application.DTOs.Auth;
using StreamForge.Domain.Entities;

namespace StreamForge.Application.Interfaces;

public interface ITokenService
{
    AuthTokenResultDto CreateTokens(User user);

    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}