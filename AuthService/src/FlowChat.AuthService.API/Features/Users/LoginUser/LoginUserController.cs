using FlowChat.API.Abstractions;
using FlowChat.AuthService.Application.Users.Commands.LoginUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.Users.LoginUser;

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
    [ProducesResponseType(typeof(LoginUserCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);

        return response.IsSuccess
            ? Ok(response.Value)
            : HandleError(response.Error);
    }
}
