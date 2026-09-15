using AutoMapper;
using FlowChat.GatewayService.Api.Features.Conversation.Interfaces;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.GetDuetConversationsWithPresence;

[ApiController]
[Authorize]
[Route("api/aggregate/conversations/duets")]
public sealed class GetDuetConversationsWithPresenceController(
    IDuetConversationsFacade duetConversationsFacade,
    IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetDuetConversationsWithPresenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDuetConversationsWithPresence(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
        {
            return Unauthorized();
        }

        var result = await duetConversationsFacade.GetDuetConversationsWithPresenceAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(mapper.Map<GetDuetConversationsWithPresenceResponse>(result.Value))
            : HandleError(result.Error);
    }
}
