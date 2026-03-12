using FlowChat.UserProfileService.Api.Features.UserProfiles;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.SetMainEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.SetMainEmail;

[ApiController]
[Route("api/userprofiles")]
public sealed class SetMainEmailController : UserProfilesControllerBase
{
    private readonly IMediator _mediator;

    public SetMainEmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{userId:guid}/emails/{emailId:guid}/main")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMainEmail(
        [FromRoute] Guid userId,
        [FromRoute] Guid emailId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetMainEmailCommand(userId, emailId), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : CreateErrorResponse(result.Error);
    }
}
