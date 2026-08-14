using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.RemoveGroupParticipants;

[ApiController]
[Authorize]
[Route("api/conversations/group/{conversationId:guid}/participants")]
public sealed class RemoveGroupParticipantsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public RemoveGroupParticipantsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveGroupParticipants(
        [FromRoute] Guid conversationId,
        [FromBody] RemoveGroupParticipantsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RemoveGroupParticipantsCommandV2(conversationId, request.ParticipantUserIds),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        return NoContent();
    }
}
