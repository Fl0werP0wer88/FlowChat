using FlowChat.API.Abstractions;
using FlowChat.AuthService.Application.Users.Commands.ConfirmUserEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.Users.ConfirmUserEmail;

[ApiController]
[Route("api/users")]
public sealed class ConfirmUserEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ConfirmUserEmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new ConfirmUserEmailCommand
            {
                UserId = userId,
                Token = token
            },
            cancellationToken);

        return response.IsSuccess
            ? NoContent()
            : HandleError(response.Error);
    }
}
