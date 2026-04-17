using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.AuthService.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace FlowChat.AuthService.Infrastructure.Services;

public sealed class OpenIddictTokenService : IOpenIddictTokenService
{
    private readonly IApiSettingsManager _apiSettingsManager;

    public OpenIddictTokenService(IApiSettingsManager apiSettingsManager)
    {
        _apiSettingsManager = apiSettingsManager;
    }

    public ClaimsPrincipal CreatePrincipal(AuthenticatedAccount account, IEnumerable<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(account);

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            ClaimTypes.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, account.Id.ToString()));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, account.Email));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.PreferredUsername, account.FriendlyUserId));
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.UniqueName, account.FriendlyUserId));

        foreach (var role in account.Roles.Where(static role => !string.IsNullOrWhiteSpace(role)))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        identity.SetDestinations(static claim => [OpenIddictConstants.Destinations.AccessToken]);

        var principal = new ClaimsPrincipal(identity);
        var grantedScopes = new HashSet<string>(
            scopes.Where(static scope => !string.IsNullOrWhiteSpace(scope)).Select(static scope => scope.Trim()),
            StringComparer.Ordinal);

        grantedScopes.Add(OpenIddictConstants.Scopes.OfflineAccess);
        grantedScopes.Add(OpenIddictConstants.Scopes.Email);
        grantedScopes.Add(OpenIddictConstants.Scopes.Profile);

        if (account.Roles.Count > 0)
        {
            grantedScopes.Add(OpenIddictConstants.Scopes.Roles);
        }

        principal.SetScopes(grantedScopes);
        principal.SetResources(_apiSettingsManager.GetJwtSettingsSection().Audience);

        return principal;
    }
}
