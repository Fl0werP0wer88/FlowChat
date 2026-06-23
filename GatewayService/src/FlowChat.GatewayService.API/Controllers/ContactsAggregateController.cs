using FlowChat.Core.Domain;
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
    private readonly IPresenceServiceClient _presenceClient;
    private readonly ILogger<ContactsAggregateController> _logger;

    public ContactsAggregateController(
        ISocialGraphServiceClient socialGraphClient,
        IChatServiceClient chatClient,
        IPresenceServiceClient presenceClient,
        ILogger<ContactsAggregateController> logger)
    {
        _socialGraphClient = socialGraphClient;
        _chatClient = chatClient;
        _presenceClient = presenceClient;
        _logger = logger;
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
        var presenceStatuses = await GetPresenceStatusesOrDefaultAsync(partnerUserIds, cancellationToken);

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
                conversationIds.TryGetValue(c.ContactUserId, out var convId) ? convId : null,
                presenceStatuses.TryGetValue(c.ContactUserId, out var presence)
                    ? presence.Status
                    : PresenceStatus.Invisible,
                presenceStatuses.TryGetValue(c.ContactUserId, out presence)
                    ? presence.ChangedAtUtc
                    : DateTimeOffset.MinValue))
            .ToList();

        return Ok(new GetContactsWithConversationsResponse(result));
    }

    private async Task<IReadOnlyDictionary<Guid, ContactPresenceStatusClientDto>> GetPresenceStatusesOrDefaultAsync(
        IReadOnlyCollection<Guid> partnerUserIds,
        CancellationToken cancellationToken)
    {
        if (partnerUserIds.Count == 0)
        {
            return new Dictionary<Guid, ContactPresenceStatusClientDto>();
        }

        try
        {
            return await _presenceClient.GetPresenceStatusesAsync(partnerUserIds, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to load contact presence statuses for aggregate contacts response.");
            return new Dictionary<Guid, ContactPresenceStatusClientDto>();
        }
    }
}
