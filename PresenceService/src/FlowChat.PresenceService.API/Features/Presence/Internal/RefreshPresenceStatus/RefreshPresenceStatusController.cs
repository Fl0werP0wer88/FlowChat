using FlowChat.PresenceService.Application.Features.Presence.Commands.RefreshPresenceStatus;
using FlowChat.PresenceService.Infrastructure.Configuration;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.RefreshPresenceStatus;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/status")]
public sealed class RefreshPresenceStatusController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public RefreshPresenceStatusController(IMediator mediator, IApiSettingsManager apiSettingsManager)
        : base(() => apiSettingsManager.GetInternalApiSettings().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(apiSettingsManager);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshPresenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new RefreshPresenceStatusCommand(request.UserIds),
            cancellationToken);

        return result.IsSuccess ? Accepted() : HandleError(result.Error);
    }
}
