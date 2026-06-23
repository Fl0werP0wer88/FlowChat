using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateGroupConversation;

[ApiController]
[Authorize]
[Route("api/conversations/group")]
public sealed class CreateGroupConversationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public CreateGroupConversationController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateGroupConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreateGroupConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateGroupConversation(
        [FromBody] CreateGroupConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new CreateGroupConversationCommand(
                request.ConversationId,
                userId,
                request.ParticipantUserIds,
                request.Name),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        var conversation = result.Value.Value;
        var response = new CreateGroupConversationResponse(
            conversation.ConversationId,
            conversation.Name,
            [.. conversation.Participants.Select(p => new ParticipantResponse(
                p.UserId, p.DisplayName, p.AvatarUrl, p.ParticipantUserId))]);

        return result.Value.WasAlreadyProcessed
            ? Ok(response)
            : StatusCode(StatusCodes.Status201Created, response);
    }
}
