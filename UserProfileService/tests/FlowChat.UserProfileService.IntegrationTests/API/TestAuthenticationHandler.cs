using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.UserProfileService.IntegrationTests.API;

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserIdHeaderName = "X-Test-UserId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeaderName, out var userIdValues)
            || !Guid.TryParse(userIdValues.ToString(), out var userId)
            || userId == Guid.Empty)
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid test user id."));
        }

        var claims = new[]
        {
            new Claim("sub", userId.ToString("D")),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString("D"))
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
