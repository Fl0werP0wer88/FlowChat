using FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.AddParticipant;

[ApiController]
[Route("api/conversations/{conversationId:guid}/participants")]
public sealed class AddParticipantController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AddParticipantController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddParticipant(
        [FromRoute] Guid conversationId,
        [FromBody] AddParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddParticipantCommand(conversationId, request.ParticipantUserIds),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        // Value=true: at least one participant newly added. Value=false: all were already members.
        return result.Value.Value
            ? Accepted()
            : Ok();
    }
}
