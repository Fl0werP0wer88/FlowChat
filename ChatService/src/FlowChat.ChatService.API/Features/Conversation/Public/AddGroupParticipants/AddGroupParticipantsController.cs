using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.AddGroupParticipants;

[ApiController]
[Authorize]
[Route("api/conversations/group/{conversationId:guid}/participants")]
public sealed class AddGroupParticipantsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AddGroupParticipantsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddGroupParticipants(
        [FromRoute] Guid conversationId,
        [FromBody] AddGroupParticipantsRequest request,
        CancellationToken cancellationToken)
    {
        // var result = await _mediator.Send(
        //     new AddGroupParticipantsCommand(conversationId, request.ParticipantUserIds),
        //     cancellationToken);
        var result = await _mediator.Send(
            new AddGroupParticipantsCommandV2(conversationId, request.ParticipantUserIds),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        return NoContent();
    }
}
