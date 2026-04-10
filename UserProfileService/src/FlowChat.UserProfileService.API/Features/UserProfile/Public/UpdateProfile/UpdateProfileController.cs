using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.UpdateProfile;

[ApiController]
[Route("api/userprofiles")]
public sealed class UpdateProfileController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UpdateProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfile(
        [FromRoute] Guid userId,
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateProfileCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.Organization,
                request.AvatarUrl,
                request.Bio,
                request.IsActive),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : HandleError(result.Error);
    }
}
