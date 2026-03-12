using FlowChat.API.Abstractions;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.SetMainEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.SetMainEmail;

[ApiController]
[Route("api/userprofiles")]
public sealed class SetMainEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SetMainEmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{userId:guid}/emails/{emailId:guid}/main")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetMainEmail(
        [FromRoute] Guid userId,
        [FromRoute] Guid emailId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetMainEmailCommand(userId, emailId), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : HandleError(result.Error);
    }
}
