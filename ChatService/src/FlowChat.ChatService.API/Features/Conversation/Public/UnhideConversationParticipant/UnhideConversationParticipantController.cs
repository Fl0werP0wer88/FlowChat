using FlowChat.ChatService.Application.Features.Conversation.Commands.UnhideConversationParticipant;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.UnhideConversationParticipant;

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}/hide")]
public sealed class UnhideConversationParticipantController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UnhideConversationParticipantController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnhideConversationParticipant(
        [FromRoute] Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new UnhideConversationParticipantCommandV2(conversationId, userId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }
}
