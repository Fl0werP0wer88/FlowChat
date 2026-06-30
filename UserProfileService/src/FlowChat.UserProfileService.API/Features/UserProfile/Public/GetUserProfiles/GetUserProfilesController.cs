using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfiles;

[ApiController]
[Authorize]
[Route("api/userprofiles")]
public sealed class GetUserProfilesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetUserProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("by-ids")]
    [ProducesResponseType(typeof(GetUserProfilesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByIds(
        [FromQuery] GetUserProfilesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserProfilesQuery(request.UserIds), cancellationToken);

        return result.IsSuccess
            ? Ok(new GetUserProfilesResponse(result.Value))
            : HandleError(result.Error);
    }
}
