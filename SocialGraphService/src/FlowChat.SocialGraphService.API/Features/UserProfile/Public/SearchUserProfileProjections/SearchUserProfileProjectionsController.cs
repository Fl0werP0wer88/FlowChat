using FlowChat.Shared.API;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Public.SearchUserProfileProjections;

[ApiController]
[Route("api/userprofiles/projections/socialgraph")]
public sealed class SearchUserProfileProjectionsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SearchUserProfileProjectionsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchUserProfileProjectionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchUserProfileProjectionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SearchUserProfileProjectionsQuery(
                request.FirstName,
                request.LastName,
                request.Organization),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new SearchUserProfileProjectionsResponse(result.Value))
            : HandleError(result.Error);
    }
}
