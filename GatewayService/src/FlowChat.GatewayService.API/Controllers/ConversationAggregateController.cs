using FlowChat.GatewayService.Api.Models;
using FlowChat.GatewayService.Api.Services;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/aggregate/conversations")]
public sealed class ConversationAggregateController : ApiControllerBase
{
    private const int DefaultMessageLimit = 20;

    private readonly IChatServiceClient _chatClient;

    public ConversationAggregateController(IChatServiceClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    [HttpPut("duet/open")]
    [ProducesResponseType(typeof(OpenDuetConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenDuetConversation(
        [FromBody] OpenDuetConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
        {
            return Unauthorized();
        }

        if (request.PartnerUserId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid partner user id.",
                Detail = "PartnerUserId is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var conversation = await _chatClient.GetDuetConversationAsync(
            request.PartnerUserId,
            cancellationToken)
            ?? await _chatClient.CreateDuetConversationAsync(request.PartnerUserId, cancellationToken);

        var messages = await _chatClient.GetConversationMessagesAsync(
            conversation.ConversationId,
            DefaultMessageLimit,
            cancellationToken);

        var response = new OpenDuetConversationResponse(
            conversation.ConversationId,
            [.. conversation.Participants.Select(p => new ConversationParticipantDto(
                p.UserId,
                p.DisplayName,
                p.AvatarUrl,
                p.ParticipantUserId))],
            [.. messages.Items.Select(message => new ConversationMessageDto(
                message.Id,
                message.ConversationId,
                message.SenderUserId,
                message.SenderDisplayName,
                message.Text,
                message.SentAtUtc))],
            messages.NextBeforeSentAtUtc,
            messages.NextBeforeMessageId,
            messages.HasMore);

        return Ok(response);
    }
}
