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
    private readonly IChatServiceClient _chatClient;
    private readonly IPresenceServiceClient _presenceClient;
    private readonly ILogger<ContactsAggregateController> _logger;

    public ContactsAggregateController(
        IChatServiceClient chatClient,
        IPresenceServiceClient presenceClient,
        ILogger<ContactsAggregateController> logger)
    {
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

        var contacts = await _chatClient.GetContactsForUserAsync(cancellationToken);

        var partnerUserIds = contacts.Select(c => c.PartnerUserId).ToList();
        var presenceStatuses = await GetPresenceStatusesOrDefaultAsync(partnerUserIds, cancellationToken);

        var result = contacts
            .Select(c => new ContactWithConversationDto(
                c.PartnerUserId,
                c.DisplayName,
                c.AvatarUrl,
                c.Email,
                c.IsBlocked,
                c.IsBlockedByPartner,
                c.IsMuted,
                c.IsHidden,
                c.ConversationId,
                c.LastReadMsgSeqNum,
                c.CurrentMsgSeqNum,
                Math.Max(0, c.CurrentMsgSeqNum - c.LastReadMsgSeqNum),
                presenceStatuses.TryGetValue(c.PartnerUserId, out var presence)
                    ? presence.Status
                    : PresenceStatus.Invisible,
                presenceStatuses.TryGetValue(c.PartnerUserId, out presence)
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
