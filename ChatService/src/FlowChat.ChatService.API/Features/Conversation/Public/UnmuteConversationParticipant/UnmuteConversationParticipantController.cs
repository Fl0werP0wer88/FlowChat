using FlowChat.ChatService.Application.Features.Conversation.Commands.UnmuteConversationParticipant;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.UnmuteConversationParticipant;

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}/mute")]
public sealed class UnmuteConversationParticipantController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UnmuteConversationParticipantController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnmuteConversationParticipant(
        [FromRoute] Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        // var result = await _mediator.Send(
        //     new UnmuteConversationParticipantCommand(conversationId, userId),
        //     cancellationToken);
        var result = await _mediator.Send(
            new UnmuteConversationParticipantCommandV2(conversationId, userId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }
}
