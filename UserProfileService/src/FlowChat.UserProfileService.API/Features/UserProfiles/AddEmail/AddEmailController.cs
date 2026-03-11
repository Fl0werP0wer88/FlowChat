using FlowChat.UserProfileService.Api.Features.UserProfiles;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.AddEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.AddEmail;

[ApiController]
[Route("api/userprofiles")]
public sealed class AddEmailController : UserProfilesControllerBase
{
    private readonly IMediator _mediator;

    public AddEmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{userId:guid}/emails")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddEmail(
        [FromRoute] Guid userId,
        [FromBody] AddEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddEmailCommand(userId, request.Address), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : CreateErrorResponse(result.Error);
    }
}
