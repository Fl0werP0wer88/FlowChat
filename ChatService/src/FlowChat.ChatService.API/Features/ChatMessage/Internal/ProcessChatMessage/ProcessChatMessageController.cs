using FlowChat.ChatService.Api.Features.ChatMessage.Internal.ProcessChatMessage;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.ProcessChatMessage;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.ProcessChatMessage;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/messages/{messageId:guid}/process")]
public sealed class ProcessChatMessageController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProcessChatMessageController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPatch]
    public async Task<IActionResult> Process(
        [FromRoute] Guid messageId,
        [FromBody] ProcessChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
            return Unauthorized();

        var result = await _mediator.Send(
            new ProcessChatMessageCommand(messageId, request.ConversationId),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
