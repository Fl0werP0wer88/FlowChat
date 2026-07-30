using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.Contact.Interfaces;
using FlowChat.GatewayService.Api.Features.Contact.Public.GetContactsWithConversations;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.GatewayService.Infrastructure.Clients.PresenceService;

namespace FlowChat.GatewayService.Api.Features.Contact.Services;

public sealed class ContactsFacade(
    IChatServiceClient chatClient,
    IPresenceServiceClient presenceClient,
    ILogger<ContactsFacade> logger) : IContactsFacade
{
    public async Task<FlowChatResult<GetContactsWithConversationsResult>> GetContactsWithConversationsAsync(
        CancellationToken cancellationToken)
    {
        var contacts = await chatClient.GetContactsForUserAsync(cancellationToken);
        var partnerUserIds = contacts.Select(contact => contact.PartnerUserId).ToList();
        var presenceStatuses = await GetPresenceStatusesOrDefaultAsync(
            partnerUserIds,
            cancellationToken);
        var items = contacts
            .Select(contact => new ContactWithConversationResult(
                contact.PartnerUserId,
                contact.DisplayName,
                contact.AvatarUrl,
                contact.Email,
                contact.IsBlocked,
                contact.IsBlockedByPartner,
                contact.IsMuted,
                contact.IsHidden,
                contact.ConversationId,
                contact.LastReadMsgSeqNum,
                contact.CurrentMsgSeqNum,
                Math.Max(0, contact.CurrentMsgSeqNum - contact.LastReadMsgSeqNum),
                presenceStatuses.TryGetValue(contact.PartnerUserId, out var presence)
                    ? presence.Status
                    : PresenceStatus.Invisible,
                presenceStatuses.TryGetValue(contact.PartnerUserId, out presence)
                    ? presence.ChangedAtUtc
                    : DateTimeOffset.MinValue))
            .ToList();

        return FlowChatResult<GetContactsWithConversationsResult>.Success(
            new GetContactsWithConversationsResult(items));
    }

    private async Task<IReadOnlyDictionary<Guid, ContactPresenceStatusClientDto>>
        GetPresenceStatusesOrDefaultAsync(
            IReadOnlyCollection<Guid> partnerUserIds,
            CancellationToken cancellationToken)
    {
        if (partnerUserIds.Count == 0)
        {
            return new Dictionary<Guid, ContactPresenceStatusClientDto>();
        }

        try
        {
            return await presenceClient.GetPresenceStatusesAsync(
                partnerUserIds,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to load contact presence statuses for aggregate contacts response.");
            return new Dictionary<Guid, ContactPresenceStatusClientDto>();
        }
    }
}
