using AutoMapper;
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
    private readonly IMapper _mapper;

    public GetUserProfilesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
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
            ? Ok(new GetUserProfilesResponse(_mapper.Map<IReadOnlyList<UserProfileResponse>>(result.Value)))
            : HandleError(result.Error);
    }
}
