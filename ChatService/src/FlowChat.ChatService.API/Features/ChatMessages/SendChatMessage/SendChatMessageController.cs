using FlowChat.Shared.API;
using FlowChat.ChatService.Application.Features.ChatMessages.Commands.SendChatMessage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.ChatMessages.SendChatMessage;

[ApiController]
[Route("api/chat/messages")]
public sealed class SendChatMessageController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SendChatMessageController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SendChatMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendChatMessage(
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SendChatMessageCommand(
                request.ConversationId,
                request.SenderUserId,
                request.SenderDisplayName,
                request.Text,
                request.RecipientUserIds),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new SendChatMessageResponse(result.Value))
            : HandleError(result.Error);
    }
}

