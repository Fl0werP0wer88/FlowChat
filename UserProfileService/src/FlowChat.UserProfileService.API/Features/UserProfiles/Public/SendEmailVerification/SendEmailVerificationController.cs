using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SendEmailVerification;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.Public.SendEmailVerification;

[ApiController]
[Route("api/userprofiles")]
public sealed class SendEmailVerificationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SendEmailVerificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{userId:guid}/emails/{emailId:guid}/verification")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendEmailVerification(
        [FromRoute] Guid userId,
        [FromRoute] Guid emailId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SendEmailVerificationCommand(userId, emailId), cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
