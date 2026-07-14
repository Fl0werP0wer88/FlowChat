using AutoMapper;
using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfiles;

[ApiController]
[Authorize]
[Route("api/userprofiles")]
public sealed class SearchUserProfilesController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SearchUserProfilesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchUserProfilesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchUserProfilesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SearchUserProfilesQuery(
                request.FirstName,
                request.LastName,
                request.Organization),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new SearchUserProfilesResponse(_mapper.Map<IReadOnlyList<UserProfileResponse>>(result.Value)))
            : HandleError(result.Error);
    }
}
