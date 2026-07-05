using AutoMapper;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.GetConversationMessages;

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversationMessages(
        [FromRoute] Guid conversationId,
        [FromQuery] int limit = DefaultLimit,
        [FromQuery] DateTimeOffset? beforeSentAtUtc = null,
        [FromQuery] Guid? beforeMessageId = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetConversationMessagesQuery(
                conversationId,
                userId,
                limit,
                beforeSentAtUtc,
                beforeMessageId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        var response = _mapper.Map<GetConversationMessagesResponse>(result.Value);

        return Ok(response);
    }
}
