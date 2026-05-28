using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.DeleteProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.DeleteProfile;

[ApiController]
[Authorize]
[Route("api/userprofiles")]
public sealed class DeleteProfileController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DeleteProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProfile(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new DeleteProfileCommand(userId), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : HandleError(result.Error);
    }
}
