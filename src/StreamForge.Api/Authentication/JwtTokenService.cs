using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Auth;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;

namespace StreamForge.Api.Authentication;

public sealed class JwtTokenService : ITokenService
{
    private const string _tokenUseClaim = "token_use";
    private const string _accessTokenUse = "access";
    private const string _refreshTokenUse = "refresh";

    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    public AuthTokenResultDto CreateTokens(User user)
    {
        var issuedAt = DateTime.UtcNow;
        var accessTokenExpiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);
        var refreshTokenExpiresAt = issuedAt.AddDays(_options.RefreshTokenDays);

        return new AuthTokenResultDto(
            CreateToken(user, accessTokenExpiresAt, _accessTokenUse),
            CreateToken(user, refreshTokenExpiresAt, _refreshTokenUse),
            accessTokenExpiresAt,
            refreshTokenExpiresAt);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        try
        {
            var principal = _tokenHandler.ValidateToken(refreshToken, CreateValidationParameters(true), out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtSecurityToken)
            {
                return null;
            }

            if (!string.Equals(jwtSecurityToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!string.Equals(principal.FindFirstValue(_tokenUseClaim), _refreshTokenUse, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private string CreateToken(User user, DateTime expiresAt, string tokenUse)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(_tokenUseClaim, tokenUse),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        return _tokenHandler.WriteToken(token);
    }

    private TokenValidationParameters CreateValidationParameters(bool validateLifetime)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingCredentials.Key,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    }
}
