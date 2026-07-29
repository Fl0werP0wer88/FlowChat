using AutoMapper;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

[ApiController]
[Authorize]
[Route("api/chat/conversations/{conversationId:guid}/messages")]
public sealed class GetConversationMessagesController : ApiControllerBase
{
    private const int DefaultLimit = 50;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetConversationMessagesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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

        var result = await _mediator.Send(
            new GetConversationMessagesQuery(conversationId, userId, limit, beforeSequenceNum),
            cancellationToken);

        return result.IsSuccess
            ? Ok(_mapper.Map<GetConversationMessagesResponse>(result.Value))
            : HandleError(result.Error);
    }
}
