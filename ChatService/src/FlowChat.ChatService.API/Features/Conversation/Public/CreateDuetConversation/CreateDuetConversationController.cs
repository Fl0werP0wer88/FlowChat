using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CreateDuetConversation;

[ApiController]
[Authorize]
[Route("api/conversations/duet")]
public sealed class CreateDuetConversationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public CreateDuetConversationController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPut]
    [ProducesResponseType(typeof(CreateDuetConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDuetConversation(
        [FromBody] CreateDuetConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new CreateDuetConversationCommand(userId, request.PartnerUserId),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var conversation = result.Value;
        var response = new CreateDuetConversationResponse(
            conversation.ConversationId,
            [.. conversation.Participants.Select(p => new ParticipantResponse(
                p.UserId, p.DisplayName, p.AvatarUrl, p.ParticipantUserId))]);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
