using FlowChat.Core.Contracts;
using FlowChat.Shared.API;
using FlowChat.RealtimeService.Application.Features.Message.Commands.PublishMessage;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishMessage;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishMessageController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishMessageController(IMediator mediator, ISettingsProvider settingsProvider)
        : base(() => settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(settingsProvider);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> Publish([FromBody] PublishMessageRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new PublishMessageCommand(
                request.MessageId,
                request.ConversationId,
                request.SenderUserId,
                request.SenderDisplayName,
                request.Text,
                request.SentAtUtc,
                request.RecipientUserIds),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}

