using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversations;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversations;

[ApiController]
[Authorize]
[Route("api/conversations")]
public sealed class GetDuetConversationsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetDuetConversationsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet("duets")]
    [ProducesResponseType(typeof(GetDuetConversationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDuetConversations(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetDuetConversationsQuery(userId), cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var response = new GetDuetConversationsResponse(
            _mapper.Map<IReadOnlyCollection<DuetConversationResponse>>(result.Value));

        return Ok(response);
    }
}
