using FlowChat.Shared.API;
using FlowChat.AuthService.Application.Features.User.Commands.LoginUser;
using Microsoft.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace FlowChat.AuthService.API.Features.User.Public.LoginUser;

[ApiController]
[Route("api/users")]
public sealed class LoginUserController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public LoginUserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest();
        }

        if (!request.IsPasswordGrantType())
        {
            return Forbid(
                CreateAuthenticationProperties(
                    OpenIddictConstants.Errors.UnsupportedGrantType,
                    "The login endpoint only supports the password grant."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var scopes = request.GetScopes().ToHashSet(StringComparer.Ordinal);
        scopes.Add(OpenIddictConstants.Scopes.OfflineAccess);

        var command = new LoginUserCommand
        {
            Login = request.Username ?? string.Empty,
            Password = request.Password ?? string.Empty,
            Scopes = scopes.ToArray()
        };
        var response = await _mediator.Send(command, cancellationToken);

        return response.IsSuccess
            ? SignIn(response.Value.Grant.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            : Forbid(
                CreateAuthenticationProperties(
                    OpenIddictConstants.Errors.InvalidGrant,
                    response.Error.ErrorMessage ?? "Invalid credentials or account is not confirmed."),
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

