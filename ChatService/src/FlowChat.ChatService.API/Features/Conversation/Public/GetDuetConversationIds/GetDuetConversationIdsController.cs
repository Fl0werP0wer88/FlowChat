using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationIds;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationIds;

[ApiController]
[Authorize]
[Route("api/conversations/duet")]
public sealed class GetDuetConversationIdsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetDuetConversationIdsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(GetDuetConversationIdsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDuetConversationIds(
        [FromBody] GetDuetConversationIdsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetDuetConversationIdsQuery(userId, request.PartnerUserIds),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new GetDuetConversationIdsResponse(result.Value))
            : HandleError(result.Error);
    }
}
