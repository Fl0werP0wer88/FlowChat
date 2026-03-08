using FlowChat.AuthService.Application.Users.Commands.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.Users.RegisterUser;

[ApiController]
[Route("api/users")]
public sealed class RegisterUserController : ControllerBase
{
    private readonly IMediator _mediator;

    public RegisterUserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        if (!response.IsSuccess)
        {
            return BadRequest(response.Error.ErrorMessage ?? "User registration failed.");
        }

        return Ok(response.Value.Id);
    }
}
