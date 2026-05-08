using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversation;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversation;

[ApiController]
[Authorize]
[Route("api/conversations/duet/detail")]
public sealed class GetDuetConversationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetDuetConversationController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetDuetConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDuetConversation(
        [FromQuery] Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetDuetConversationQuery(userId, partnerUserId),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var conversation = result.Value;
        var response = new GetDuetConversationResponse(
            conversation.ConversationId,
            [.. conversation.Participants.Select(p => new ParticipantResponse(
                p.UserId, p.DisplayName, p.AvatarUrl, p.ParticipantUserId))]);

        return Ok(response);
    }
}
