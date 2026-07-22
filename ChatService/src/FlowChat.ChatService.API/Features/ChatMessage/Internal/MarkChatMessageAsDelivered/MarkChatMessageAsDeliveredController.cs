using FlowChat.ChatService.Api.Features.ChatMessage.Internal.MarkChatMessageAsDelivered;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.MarkChatMessageAsDelivered;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/messages/{messageId:guid}/delivery")]
public sealed class MarkChatMessageAsDeliveredController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public MarkChatMessageAsDeliveredController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPatch]
    public async Task<IActionResult> MarkAsDelivered(
        [FromRoute] Guid messageId,
        [FromBody] MarkChatMessageAsDeliveredRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
            return Unauthorized();

        // var result = await _mediator.Send(
        //     new MarkChatMessageAsDeliveredCommand(messageId, request.ConversationId, request.DeliveredAtUtc),
        //     cancellationToken);
        var result = await _mediator.Send(
            new MarkChatMessageAsDeliveredCommandV2(messageId, request.ConversationId, request.DeliveredAtUtc),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
