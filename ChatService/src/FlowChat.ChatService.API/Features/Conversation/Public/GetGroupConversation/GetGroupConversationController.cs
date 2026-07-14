using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversation;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversation;

[ApiController]
[Authorize]
[Route("api/conversations/group")]
public sealed class GetGroupConversationController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetGroupConversationController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet("{conversationId:guid}")]
    [ProducesResponseType(typeof(GetGroupConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGroupConversation(
        [FromRoute] Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetGroupConversationQuery(conversationId),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var response = _mapper.Map<GetGroupConversationResponse>(result.Value);

        return Ok(response);
    }
}
