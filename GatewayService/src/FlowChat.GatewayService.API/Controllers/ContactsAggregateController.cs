using FlowChat.GatewayService.Api.Models;
using FlowChat.GatewayService.Api.Services;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/aggregate")]
public sealed class ContactsAggregateController : ApiControllerBase
{
    private readonly ISocialGraphServiceClient _socialGraphClient;
    private readonly IChatServiceClient _chatClient;

    public ContactsAggregateController(
        ISocialGraphServiceClient socialGraphClient,
        IChatServiceClient chatClient)
    {
        _socialGraphClient = socialGraphClient;
        _chatClient = chatClient;
    }

    [HttpGet("contacts")]
    [ProducesResponseType(typeof(GetContactsWithConversationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetContactsWithConversations(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
        {
            return Unauthorized();
        }

        var contacts = await _socialGraphClient.GetContactsAsync(cancellationToken);

        var partnerUserIds = contacts.Select(c => c.ContactUserId).ToList();

        var conversationIds = partnerUserIds.Count > 0
            ? await _chatClient.GetDuetConversationIdsAsync(partnerUserIds, cancellationToken)
            : (IReadOnlyDictionary<Guid, Guid>)new Dictionary<Guid, Guid>();

        var result = contacts
            .Select(c => new ContactWithConversationDto(
                c.Id,
                c.ContactUserId,
                c.DisplayName,
                c.FirstName,
                c.LastName,
                c.PhoneNumber,
                c.Email,
                c.IsBlocked,
                conversationIds.TryGetValue(c.ContactUserId, out var convId) ? convId : null))
            .ToList();

        return Ok(new GetContactsWithConversationsResponse(result));
    }
}
