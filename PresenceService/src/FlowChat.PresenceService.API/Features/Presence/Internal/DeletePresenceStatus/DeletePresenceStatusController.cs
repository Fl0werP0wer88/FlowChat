using FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Contracts;
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

    public DeletePresenceStatusController(IMediator mediator, ISettingsProvider settingsProvider)
        : base(() => settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(settingsProvider);
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
