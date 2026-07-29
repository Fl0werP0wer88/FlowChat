using AutoMapper;
using FlowChat.Shared.API;
using MediatR;
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
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public CatchUpConversationMessagesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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

        var result = await _mediator.Send(
            new CatchUpConversationMessagesQuery(
                conversationId,
                userId,
                limit,
                afterSequenceNum,
                throughSequenceNum),
            cancellationToken);

        return result.IsSuccess
            ? Ok(_mapper.Map<CatchUpConversationMessagesResponse>(result.Value))
            : HandleError(result.Error);
    }
}
