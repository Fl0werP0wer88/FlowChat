using FlowChat.RealtimeService.Application.Messages.Commands.ReceiveMessage;
using FlowChat.RealtimeService.Application.Presence.Commands.PresenceChanged;
using FlowChat.RealtimeService.Application.Realtime.Contracts;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.RealtimeService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.RealtimeService.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class InternalRealtimeController(IMediator mediator, IApiSettingsManager apiSettingsManager) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IApiSettingsManager _apiSettingsManager = apiSettingsManager
        ?? throw new ArgumentNullException(nameof(apiSettingsManager));

    [HttpPost("messages")]
    public async Task<IActionResult> ReceiveMessage([FromBody] ReceiveMessageRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        await _mediator.Send(
            new ReceiveMessageCommand(
                request.MessageId,
                request.ConversationId,
                request.SenderUserId,
                request.SenderDisplayName,
                request.Text,
                request.SentAtUtc,
                request.RecipientUserIds),
            cancellationToken);

        return Accepted();
    }

    [HttpPost("presence")]
    public async Task<IActionResult> PresenceChanged([FromBody] PresenceChangedRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        await _mediator.Send(
            new PresenceChangedCommand(
                request.UserId,
                request.Status,
                request.ChangedAtUtc,
                request.RecipientUserIds),
            cancellationToken);

        return Accepted();
    }

    private bool HasValidInternalApiKey()
    {
        var expectedApiKey = _apiSettingsManager.GetInternalApiSettings().ApiKey;
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return false;
        }

        if (!Request.Headers.TryGetValue(RealtimeInternalApiClient.ApiKeyHeaderName, out var providedApiKey))
        {
            return false;
        }

        return string.Equals(providedApiKey.ToString(), expectedApiKey, StringComparison.Ordinal);
    }
}
