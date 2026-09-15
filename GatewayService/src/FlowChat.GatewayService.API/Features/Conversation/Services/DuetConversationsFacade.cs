using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.Conversation.Interfaces;
using FlowChat.GatewayService.Api.Features.Conversation.Public.GetDuetConversationsWithPresence;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.GatewayService.Infrastructure.Clients.PresenceService;

namespace FlowChat.GatewayService.Api.Features.Conversation.Services;

public sealed class DuetConversationsFacade(
    IChatServiceClient chatClient,
    IPresenceServiceClient presenceClient,
    ILogger<DuetConversationsFacade> logger) : IDuetConversationsFacade
{
    public async Task<FlowChatResult<GetDuetConversationsWithPresenceResult>> GetDuetConversationsWithPresenceAsync(
        CancellationToken cancellationToken)
    {
        var conversations = await chatClient.GetDuetConversationsAsync(cancellationToken);
        var partnerUserIds = conversations.Select(conversation => conversation.PartnerUserId).ToList();
        var presenceStatuses = await GetPresenceStatusesOrDefaultAsync(
            partnerUserIds,
            cancellationToken);
        var items = conversations
            .Select(conversation => new DuetConversationWithPresenceResult(
                conversation.PartnerUserId,
                conversation.DisplayName,
                conversation.AvatarUrl,
                conversation.Email,
                conversation.IsBlocked,
                conversation.IsBlockedByPartner,
                conversation.IsMuted,
                conversation.IsHidden,
                conversation.ConversationId,
                conversation.LastReadMsgSeqNum,
                conversation.CurrentMsgSeqNum,
                Math.Max(0, conversation.CurrentMsgSeqNum - conversation.LastReadMsgSeqNum),
                presenceStatuses.TryGetValue(conversation.PartnerUserId, out var presence)
                    ? presence.Status
                    : PresenceStatus.Invisible,
                presenceStatuses.TryGetValue(conversation.PartnerUserId, out presence)
                    ? presence.ChangedAtUtc
                    : DateTimeOffset.MinValue))
            .ToList();

        return FlowChatResult<GetDuetConversationsWithPresenceResult>.Success(
            new GetDuetConversationsWithPresenceResult(items));
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
                "Failed to load presence statuses for duet conversations response.");
            return new Dictionary<Guid, ContactPresenceStatusClientDto>();
        }
    }
}
