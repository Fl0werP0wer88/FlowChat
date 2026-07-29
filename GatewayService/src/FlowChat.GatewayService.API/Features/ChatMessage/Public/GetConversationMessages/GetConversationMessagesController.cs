using AutoMapper;
using FlowChat.GatewayService.Api.Features.ChatMessage.Interfaces;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

[ApiController]
[Authorize]
[Route("api/chat/conversations/{conversationId:guid}/messages")]
public sealed class GetConversationMessagesController : ApiControllerBase
{
    private const int DefaultLimit = 50;
    private readonly IConversationMessagesFacade _messagesFacade;
    private readonly IMapper _mapper;

    public GetConversationMessagesController(
        IConversationMessagesFacade messagesFacade,
        IMapper mapper)
    {
        _messagesFacade = messagesFacade ?? throw new ArgumentNullException(nameof(messagesFacade));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetConversationMessagesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationMessages(
        [FromRoute] Guid conversationId,
        [FromQuery] int limit = DefaultLimit,
        [FromQuery] long? beforeSequenceNum = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _messagesFacade.GetHistoryAsync(
            conversationId,
            userId,
            limit,
            beforeSequenceNum,
            cancellationToken);

        return result.IsSuccess
            ? Ok(_mapper.Map<GetConversationMessagesResponse>(result.Value))
            : HandleError(result.Error);
    }
}
