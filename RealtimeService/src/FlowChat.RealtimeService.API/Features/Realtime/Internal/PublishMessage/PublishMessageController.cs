using FlowChat.Shared.API;
using FlowChat.RealtimeService.Application.Features.Message.Commands.PublishMessage;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishMessage;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishMessageController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishMessageController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost("messages/direct")]
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
                request.Text,
                request.SequenceNum,
                request.SentAtUtc,
                request.DeliveredAtUtc),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
