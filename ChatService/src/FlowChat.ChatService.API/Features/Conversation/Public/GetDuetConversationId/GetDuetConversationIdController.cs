using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationId;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationId;

[ApiController]
[Authorize]
[Route("api/conversations/duet")]
public sealed class GetDuetConversationIdController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetDuetConversationIdController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetDuetConversationIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDuetConversationId(
        [FromQuery] Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetDuetConversationIdQuery(userId, partnerUserId),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new GetDuetConversationIdResponse(result.Value))
            : HandleError(result.Error);
    }
}
