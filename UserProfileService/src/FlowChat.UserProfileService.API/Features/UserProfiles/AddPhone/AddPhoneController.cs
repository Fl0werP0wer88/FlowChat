using FlowChat.UserProfileService.Api.Features.UserProfiles;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.AddPhone;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.AddPhone;

[ApiController]
[Route("api/userprofiles")]
public sealed class AddPhoneController : UserProfilesControllerBase
{
    private readonly IMediator _mediator;

    public AddPhoneController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{userId:guid}/phones")]
    [ProducesResponseType(typeof(AddPhoneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPhone(
        [FromRoute] Guid userId,
        [FromBody] AddPhoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddPhoneCommand(userId, request.Number), cancellationToken);

        return result.IsSuccess
            ? Ok(new AddPhoneResponse(result.Value))
            : CreateErrorResponse(result.Error);
    }
}
