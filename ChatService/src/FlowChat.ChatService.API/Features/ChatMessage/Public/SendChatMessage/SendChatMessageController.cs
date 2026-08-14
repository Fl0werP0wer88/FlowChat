using FlowChat.Shared.API;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.SendChatMessage;

[ApiController]
[Authorize]
[Route("api/chat/messages")]
public sealed class SendChatMessageController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SendChatMessageController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPut]
    [ProducesResponseType(typeof(SendChatMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendChatMessage(
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new SendChatMessageCommandV2(
                request.Id,
                request.ConversationId,
                userId,
                request.Text),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        var response = new SendChatMessageResponse(
            result.Value.MessageId,
            result.Value.SentAtUtc,
            result.Value.SequenceNum);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
