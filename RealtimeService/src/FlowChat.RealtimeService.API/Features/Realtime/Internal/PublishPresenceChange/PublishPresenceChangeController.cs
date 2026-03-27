using FlowChat.Shared.API;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishPresenceChange;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishPresenceChangeController(IMediator mediator, IApiSettingsManager apiSettingsManager) : ApiControllerBase
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IApiSettingsManager _apiSettingsManager = apiSettingsManager
        ?? throw new ArgumentNullException(nameof(apiSettingsManager));

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

    private bool HasValidInternalApiKey()
    {
        var expectedApiKey = _apiSettingsManager.GetInternalApiSettings().ApiKey;
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return false;
        }

        if (!Request.Headers.TryGetValue(InternalApiKeyHeaderName, out var providedApiKey))
        {
            return false;
        }

        return string.Equals(providedApiKey.ToString(), expectedApiKey, StringComparison.Ordinal);
    }
}

