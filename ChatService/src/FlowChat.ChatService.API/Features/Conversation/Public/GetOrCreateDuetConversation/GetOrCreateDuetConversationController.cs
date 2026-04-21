using FlowChat.ChatService.Application.Features.Conversation.Commands.GetOrCreateDuetConversation;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetOrCreateDuetConversation;

[ApiController]
[Route("api/conversations/duet")]
public sealed class GetOrCreateDuetConversationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetOrCreateDuetConversationController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(GetOrCreateDuetConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrCreateDuetConversation(
        [FromBody] GetOrCreateDuetConversationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetOrCreateDuetConversationCommand(request.RequestingUserId, request.PartnerUserId),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var response = new GetOrCreateDuetConversationResponse(
            result.Value.ConversationId,
            [.. result.Value.Participants.Select(p => new ParticipantResponse(
                p.UserId, p.DisplayName, p.AvatarUrl, p.FriendlyUserId))]);

        return Ok(response);
    }
}
