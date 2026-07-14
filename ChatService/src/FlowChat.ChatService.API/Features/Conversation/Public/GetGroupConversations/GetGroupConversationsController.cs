using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversations;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversations;

[ApiController]
[Authorize]
[Route("api/conversations/group")]
public sealed class GetGroupConversationsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetGroupConversationsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetGroupConversationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGroupConversations(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetGroupConversationsQuery(userId),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var response = new GetGroupConversationsResponse(
            _mapper.Map<IReadOnlyCollection<GroupConversationSummaryResponse>>(result.Value));

        return Ok(response);
    }
}
