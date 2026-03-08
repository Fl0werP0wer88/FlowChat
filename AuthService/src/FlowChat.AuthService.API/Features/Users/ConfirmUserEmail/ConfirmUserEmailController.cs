using FlowChat.AuthService.Application.Users.Commands.ConfirmUserEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.Users.ConfirmUserEmail;

[ApiController]
[Route("api/users")]
public sealed class ConfirmUserEmailController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfirmUserEmailController(IMediator mediator)
    {
        _mediator = mediator;
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
