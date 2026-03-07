using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Application.Models;
using Microsoft.IdentityModel.Tokens;

namespace FlowChat.AuthService.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IApiSettingsManager _apiSettingsManager;

    public JwtTokenGenerator(IApiSettingsManager apiSettingsManager)
    {
        _apiSettingsManager = apiSettingsManager;
    }

    public JwtTokenResult GenerateToken(AuthenticatedUser user)
    {
        var jwtSettings = _apiSettingsManager.GetJwtSettings();
        var key = jwtSettings.Key;
        var issuer = jwtSettings.Issuer;
        var audience = jwtSettings.Audience;
        var expiresMinutes = jwtSettings.ExpiresMinutes;

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Key.");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Audience.");
        }

        var tokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: tokenExpiresAtUtc,
            signingCredentials: credentials);

        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult
        {
            AccessToken = serializedToken,
            ExpiresAtUtc = tokenExpiresAtUtc
        };
    }
}
