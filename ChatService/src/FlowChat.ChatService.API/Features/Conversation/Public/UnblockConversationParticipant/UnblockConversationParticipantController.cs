using FlowChat.ChatService.Application.Features.Conversation.Commands.UnblockConversationParticipant;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.UnblockConversationParticipant;

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}/block")]
public sealed class UnblockConversationParticipantController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UnblockConversationParticipantController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnblockConversationParticipant(
        [FromRoute] Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        // var result = await _mediator.Send(
        //     new UnblockConversationParticipantCommand(conversationId, userId),
        //     cancellationToken);
        var result = await _mediator.Send(
            new UnblockConversationParticipantCommandV2(conversationId, userId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }
}
