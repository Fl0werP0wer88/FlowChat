using FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;
using FlowChat.PresenceService.Infrastructure.Configuration;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.DeletePresenceStatus;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/status")]
public sealed class DeletePresenceStatusController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DeletePresenceStatusController(IMediator mediator, IApiSettingsManager apiSettingsManager)
        : base(() => apiSettingsManager.GetInternalApiSettingsSection().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(apiSettingsManager);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(
        [FromBody] DeletePresenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new DeletePresenceStatusCommand(request.UserId),
            cancellationToken);

        return result.IsSuccess ? Accepted() : HandleError(result.Error);
    }
}
