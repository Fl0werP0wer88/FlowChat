using System.Security.Claims;
using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserStatus;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.Presence.Public.ChangeUserStatus;

[ApiController]
[Authorize]
[Route("api/presence/status")]
public sealed class ChangeUserStatusController(IMediator mediator) : ApiControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [HttpPut]
    public async Task<IActionResult> Change(
        [FromBody] ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new ChangeUserStatusCommand(userId, request.Status),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }
}
