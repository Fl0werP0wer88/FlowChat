using FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversations;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversations;

[ApiController]
[Authorize]
[Route("api/conversations/group")]
public sealed class GetGroupConversationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetGroupConversationsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetGroupConversationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGroupConversations(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetGroupConversationsQuery(userId),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var response = new GetGroupConversationsResponse(
            [.. result.Value.Select(c => new GroupConversationSummaryResponse(
                c.ConversationId,
                c.Name,
                c.ParticipantCount,
                c.LastReadMsgSeqNum,
                c.CurrentMsgSeqNum))]);

        return Ok(response);
    }
}
