using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.ConfirmEmailVerification;

[ApiController]
[Route("api/userprofiles/email-verification")]
public sealed class ConfirmEmailVerificationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ConfirmEmailVerificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Confirm(
        [FromBody] ConfirmEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConfirmEmailVerificationCommand(request.Token), cancellationToken);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return result.Value.WasAlreadyProcessed
            ? Ok()
            : NoContent();
    }
}
