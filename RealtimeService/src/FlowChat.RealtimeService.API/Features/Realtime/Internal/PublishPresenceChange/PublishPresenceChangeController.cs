using FlowChat.Core.Contracts;
using FlowChat.Shared.API;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishPresenceChange;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishPresenceChangeController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishPresenceChangeController(IMediator mediator, ISettingsProvider settingsProvider)
        : base(() => settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(settingsProvider);
    }

    [HttpPost("presence")]
    public async Task<IActionResult> Publish([FromBody] PublishPresenceChangeRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new PublishPresenceChangeCommand(
                request.UserId,
                request.Status,
                request.ChangedAtUtc,
                request.RecipientUserIds),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}

