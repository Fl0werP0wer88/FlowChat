using FlowChat.UserProfileService.Api.Features.UserProfiles;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.SetMainPhone;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.SetMainPhone;

[ApiController]
[Route("api/userprofiles")]
public sealed class SetMainPhoneController : UserProfilesControllerBase
{
    private readonly IMediator _mediator;

    public SetMainPhoneController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{userId:guid}/phones/{phoneId:guid}/main")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMainPhone(
        [FromRoute] Guid userId,
        [FromRoute] Guid phoneId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetMainPhoneCommand(userId, phoneId), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : CreateErrorResponse(result.Error);
    }
}
