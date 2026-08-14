using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CopyDuetAsGroup;

[ApiController]
[Authorize]
[Route("api/conversations/duet/copy-as-group")]
public sealed class CopyDuetAsGroupController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public CopyDuetAsGroupController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CopyDuetAsGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CopyDuetAsGroup(
        [FromBody] CopyDuetAsGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new CreateGroupFromDuetCommandV2(request.NewGroupConversationId, userId, request.PartnerUserId),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var response = _mapper.Map<CopyDuetAsGroupResponse>(result.Value);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
