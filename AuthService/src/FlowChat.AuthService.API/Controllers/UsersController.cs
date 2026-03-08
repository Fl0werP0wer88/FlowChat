using FlowChat.AuthService.Application.Commands;
using FlowChat.AuthService.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
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

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.IsSuccess)
        {
            return Unauthorized(response.Error.ErrorMessage ?? "Invalid credentials or account is not confirmed.");
        }

        return Ok(response.Value);
    }

    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ConfirmUserEmailCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new ConfirmUserEmailCommand
            {
                UserId = userId,
                Token = token
            },
            cancellationToken);

        if (!response.IsSuccess)
        {
            return BadRequest(response.Error.ErrorMessage ?? "Email confirmation failed.");
        }

        return Ok(response.Value);
    }
}
