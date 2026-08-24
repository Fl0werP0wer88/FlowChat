using AutoMapper;
using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfilesRangeAscending;

[ApiController]
[Authorize]
[Route("api/userprofiles/search/range/ascending")]
public sealed class SearchUserProfilesRangeAscendingController : ApiControllerBase
{
    public const int DefaultLimit = 20;

    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SearchUserProfilesRangeAscendingController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    [ProducesResponseType(typeof(SearchUserProfilesRangeAscendingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchRangeAscending(
        [FromQuery] SearchUserProfilesRangeAscendingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SearchUserProfilesRangeAscendingQuery(
                request.FirstName,
                request.LastName,
                request.Organization,
                request.Cursor,
                request.Limit),
            cancellationToken);

        return result.IsSuccess
            ? Ok(_mapper.Map<SearchUserProfilesRangeAscendingResponse>(result.Value))
            : HandleError(result.Error);
    }
}
