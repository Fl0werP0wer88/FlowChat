using FlowChat.RealtimeService.Application.Messages.Commands.PublishMessage;
using FlowChat.RealtimeService.Application.Presence.Commands.PublishPresenceChange;
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
    public async Task<IActionResult> PublishMessage([FromBody] PublishMessageRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        await _mediator.Send(
            new PublishMessageCommand(
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
    public async Task<IActionResult> PublishPresenceChange([FromBody] PublishPresenceChangeRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        await _mediator.Send(
            new PublishPresenceChangeCommand(
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
