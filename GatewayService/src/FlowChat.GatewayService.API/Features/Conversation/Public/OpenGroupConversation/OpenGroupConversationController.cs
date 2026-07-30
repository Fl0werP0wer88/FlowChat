using AutoMapper;
using FlowChat.GatewayService.Api.Features.Conversation.Interfaces;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Features.Conversation.Public.OpenGroupConversation;

[ApiController]
[Authorize]
[Route("api/aggregate/conversations/group/open")]
public sealed class OpenGroupConversationController(
    IConversationFacade conversationFacade,
    IMapper mapper) : ApiControllerBase
{
    [HttpPut]
    [ProducesResponseType(typeof(OpenGroupConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenGroupConversation(
        [FromBody] OpenGroupConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await conversationFacade.OpenGroupAsync(
            userId,
            request.ConversationId,
            cancellationToken);

        return result.IsSuccess
            ? Ok(mapper.Map<OpenGroupConversationResponse>(result.Value))
            : HandleError(result.Error);
    }
}
