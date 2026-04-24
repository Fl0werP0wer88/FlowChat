using FlowChat.PresenceService.Application.Features.Presence.Queries.GetUserPresencePreferences;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.Presence.Public.GetUserPresencePreferences;

[ApiController]
[Authorize]
[Route("api/presence/preferences")]
public sealed class GetUserPresencePreferencesController(IMediator mediator) : ApiControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetUserPresencePreferencesQuery(userId),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new UserPresencePreferencesResponse { PreferredStatus = result.Value })
            : HandleError(result.Error);
    }


}
