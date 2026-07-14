using FlowChat.ChatService.Api.Features.ChatMessage.Internal.SetChatMessageSequenceNumber;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.SetChatMessageSequenceNumber;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/messages/{messageId:guid}/sequence-number")]
public sealed class SetChatMessageSequenceNumberController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SetChatMessageSequenceNumberController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPatch]
    public async Task<IActionResult> SetSequenceNumber(
        [FromRoute] Guid messageId,
        [FromBody] SetChatMessageSequenceNumberRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
            return Unauthorized();

        var result = await _mediator.Send(
            new SetChatMessageSequenceNumberCommand(messageId, request.ConversationId),
            cancellationToken);

        return result.IsSuccess
            ? Accepted(new SetChatMessageSequenceNumberResponse { SequenceNum = result.Value })
            : HandleError(result.Error);
    }
}
