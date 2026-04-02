using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetAuthEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SetAuthEmail;

[ApiController]
[Route("api/userprofiles")]
public sealed class SetAuthEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SetAuthEmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{userId:guid}/emails/{emailId:guid}/auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetAuthEmail(
        [FromRoute] Guid userId,
        [FromRoute] Guid emailId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetAuthEmailCommand(userId, emailId), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : HandleError(result.Error);
    }
}
