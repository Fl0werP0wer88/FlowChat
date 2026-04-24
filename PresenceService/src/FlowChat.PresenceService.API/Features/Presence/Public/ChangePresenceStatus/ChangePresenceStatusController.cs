using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.Presence.Public.ChangePresenceStatus;

[ApiController]
[Authorize]
[Route("api/presence/status")]
public sealed class ChangePresenceStatusController(IMediator mediator) : ApiControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [HttpPut]
    public async Task<IActionResult> Change(
        [FromBody] ChangePresenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new ChangePresenceStatusCommand(userId, request.Status),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }


}
