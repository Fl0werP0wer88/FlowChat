using AutoMapper;
using FlowChat.GatewayService.Api.Features.Conversation.Interfaces;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenDuetConversation;

[ApiController]
[Authorize]
[Route("api/aggregate/conversations/duet/open")]
public sealed class OpenDuetConversationController(
    IConversationFacade conversationFacade,
    IMapper mapper) : ApiControllerBase
{
    [HttpPut]
    [ProducesResponseType(typeof(OpenDuetConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenDuetConversation(
        [FromBody] OpenDuetConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await conversationFacade.OpenDuetAsync(
            userId,
            request.PartnerUserId,
            request.KnownConversationId,
            cancellationToken);

        return result.IsSuccess
            ? Ok(mapper.Map<OpenDuetConversationResponse>(result.Value))
            : HandleError(result.Error);
    }
}
