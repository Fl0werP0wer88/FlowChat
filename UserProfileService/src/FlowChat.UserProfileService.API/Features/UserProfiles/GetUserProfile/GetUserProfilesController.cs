using FlowChat.API.Abstractions;
using FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.GetUserProfile;

[ApiController]
[Route("api/userprofiles")]
public sealed class UserProfilesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UserProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new GetUserProfileQuery(userId), cancellationToken);

        if (profile is null)
        {
            return NotFoundResponse($"User profile '{userId}' was not found.");
        }

        return Ok(profile);
    }
}
