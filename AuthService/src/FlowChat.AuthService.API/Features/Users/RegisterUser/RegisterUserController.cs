using FlowChat.API.Abstractions;
using FlowChat.AuthService.Application.Users.Commands.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.Users.RegisterUser;

[ApiController]
[Route("api/users")]
public sealed class RegisterUserController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public RegisterUserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);

        return response.IsSuccess
            ? Ok(response.Value.Id)
            : HandleError(response.Error);
    }
}
