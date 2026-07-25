using FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.MarkConversationAsRead;

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}/read-state")]
public sealed class MarkConversationAsReadController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public MarkConversationAsReadController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkConversationAsRead(
        [FromRoute] Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new MarkConversationAsReadCommandV2(conversationId, userId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }
}
