using FlowChat.AuthService.Application.Commands;
using FlowChat.Domain.Abstractions;
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
    public async Task<ActionResult<Guid>> Create([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);

        if (response.IsFailure)
        {
            return MapError(response.Error);
        }

        return Ok(response.Value.Id);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);

        if (response.IsFailure)
        {
            return MapError(response.Error);
        }

        if (!response.Value.IsSuccess)
        {
            return Unauthorized("Invalid credentials or account is not confirmed.");
        }

        return Ok(response.Value);
    }

    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
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

        if (response.IsFailure)
        {
            return MapError(response.Error);
        }

        if (!response.Value.IsSuccess)
        {
            return BadRequest("Email confirmation failed.");
        }

        return Ok("Email confirmed.");
    }

    private ActionResult MapError(IDomainError error)
    {
        if (error.ErrorType == ErrorType.Validation)
        {
            return BadRequest(new { error.ErrorMessage, error.Errors });
        }

        if (error.ErrorType == ErrorType.BadRequest)
        {
            return BadRequest(error.ErrorMessage);
        }

        if (error.ErrorType == ErrorType.NotFound)
        {
            return NotFound(error.ErrorMessage);
        }

        if (error.ErrorType == ErrorType.Conflict)
        {
            return Conflict(error.ErrorMessage);
        }

        return StatusCode(StatusCodes.Status500InternalServerError, error.ErrorMessage);
    }
}
