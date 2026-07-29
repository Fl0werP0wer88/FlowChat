using AutoMapper;
using FlowChat.GatewayService.Api.Features.ChatMessage.Interfaces;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

[ApiController]
[Authorize]
[Route("api/chat/conversations/{conversationId:guid}/messages/catch-up")]
public sealed class CatchUpConversationMessagesController : ApiControllerBase
{
    private const int DefaultLimit = 100;
    private readonly IConversationMessagesFacade _messagesFacade;
    private readonly IMapper _mapper;

    public CatchUpConversationMessagesController(
        IConversationMessagesFacade messagesFacade,
        IMapper mapper)
    {
        _messagesFacade = messagesFacade ?? throw new ArgumentNullException(nameof(messagesFacade));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    [ProducesResponseType(typeof(CatchUpConversationMessagesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CatchUpConversationMessages(
        [FromRoute] Guid conversationId,
        [FromQuery, BindRequired] long afterSequenceNum,
        [FromQuery] long? throughSequenceNum = null,
        [FromQuery] int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _messagesFacade.CatchUpAsync(
            conversationId,
            userId,
            limit,
            afterSequenceNum,
            throughSequenceNum,
            cancellationToken);

        return result.IsSuccess
            ? Ok(_mapper.Map<CatchUpConversationMessagesResponse>(result.Value))
            : HandleError(result.Error);
    }
}
