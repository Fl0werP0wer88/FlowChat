using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.GetConversationMessages;

[ApiController]
[Route("api/chat/conversations/{conversationId:guid}/messages")]
public sealed class GetConversationMessagesController : ApiControllerBase
{
    private const int DefaultLimit = 50;
    private readonly IMediator _mediator;

    public GetConversationMessagesController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetConversationMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversationMessages(
        [FromRoute] Guid conversationId,
        [FromQuery] Guid requestingUserId,
        [FromQuery] int limit = DefaultLimit,
        [FromQuery] DateTimeOffset? beforeSentAtUtc = null,
        [FromQuery] Guid? beforeMessageId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetConversationMessagesQuery(
                conversationId,
                requestingUserId,
                limit,
                beforeSentAtUtc,
                beforeMessageId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        var response = new GetConversationMessagesResponse(
            [.. result.Value.Items.Select(message => new ChatMessageResponse(
                message.Id,
                message.ConversationId,
                message.SenderUserId,
                message.SenderDisplayName,
                message.Text,
                message.SentAtUtc))],
            result.Value.NextBeforeSentAtUtc,
            result.Value.NextBeforeMessageId,
            result.Value.HasMore);

        return Ok(response);
    }
}
