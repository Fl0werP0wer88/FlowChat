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

    public CopyDuetAsGroupController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CopyDuetAsGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CopyDuetAsGroupResponse), StatusCodes.Status200OK)]
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
            new CreateGroupFromDuetCommand(request.NewGroupConversationId, userId, request.PartnerUserId),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var conversation = result.Value.Value;
        var response = new CopyDuetAsGroupResponse(
            conversation.ConversationId,
            conversation.Name,
            [.. conversation.Participants.Select(p => new ParticipantResponse(
                p.UserId, p.DisplayName, p.AvatarUrl, p.ParticipantUserId))]);

        return result.Value.WasAlreadyProcessed
            ? Ok(response)
            : StatusCode(StatusCodes.Status201Created, response);
    }
}
