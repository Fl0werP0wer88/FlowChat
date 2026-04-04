using FlowChat.Shared.API;
using FlowChat.AuthService.Application.Features.User.Commands.RefreshToken;
using Microsoft.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace FlowChat.AuthService.API.Features.User.Public.RefreshToken;

[ApiController]
[Route("api/users")]
public sealed class RefreshTokenController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public RefreshTokenController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("refresh-token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest();
        }

        if (!request.IsRefreshTokenGrantType())
        {
            return Forbid(
                CreateAuthenticationProperties(
                    OpenIddictConstants.Errors.UnsupportedGrantType,
                    "The refresh endpoint only supports the refresh_token grant."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = authenticateResult.Principal;
        var subject = principal?.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;

        if (!authenticateResult.Succeeded || !Guid.TryParse(subject, out var accountId))
        {
            return Forbid(
                CreateAuthenticationProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    "Invalid or expired refresh token."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var command = new RefreshTokenCommand
        {
            AccountId = accountId,
            Scopes = principal!.GetScopes().ToArray()
        };
        var response = await _mediator.Send(command, cancellationToken);

        return response.IsSuccess
            ? SignIn(response.Value.Grant.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            : Forbid(
                CreateAuthenticationProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    response.Error.ErrorMessage ?? "User not found or account is not confirmed."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static AuthenticationProperties CreateAuthenticationProperties(string error, string description)
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        });
    }
}
