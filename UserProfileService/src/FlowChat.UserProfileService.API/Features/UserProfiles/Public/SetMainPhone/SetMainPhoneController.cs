using FlowChat.API.Abstractions;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.SetMainPhone;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.Public.SetMainPhone;

[ApiController]
[Route("api/userprofiles")]
public sealed class SetMainPhoneController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SetMainPhoneController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{userId:guid}/phones/{phoneId:guid}/main")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetMainPhone(
        [FromRoute] Guid userId,
        [FromRoute] Guid phoneId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetMainPhoneCommand(userId, phoneId), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : HandleError(result.Error);
    }
}
