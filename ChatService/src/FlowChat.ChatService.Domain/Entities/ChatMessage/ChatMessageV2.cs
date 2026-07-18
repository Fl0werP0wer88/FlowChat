using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage;

public sealed class ChatMessageV2 : AggregateRootBase<ChatMessageV2>
{
    public Id<ConversationV2> ConversationId { get; private set; }
    public Id<UserProfileMarker> SenderUserId { get; private set; }
    public string Text { get; private set; }
    public UtcDateTimeOffset SentAtUtc { get; private set; }
    public UtcDateTimeOffset? DeliveredAtUtc { get; private set; }
    private Guid[] _recipientUserIds = [];
    public IReadOnlyCollection<Id<UserProfileMarker>> RecipientUserIds =>
        [.. _recipientUserIds.Select(Id<UserProfileMarker>.FromGuid)];
    public long? SequenceNum { get; private set; }
    public DeliveryStatus DeliveryStatus { get; private set; }

    private ChatMessageV2(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        UtcDateTimeOffset sentAtUtc,
        Guid[] recipientUserIds,
        long? sequenceNum = null,
        DeliveryStatus deliveryStatus = DeliveryStatus.Pending,
        UtcDateTimeOffset? deliveredAtUtc = null) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(senderUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (sequenceNum is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequenceNum),
                "Sequence number must be greater than zero.");
        }

        if (!Enum.IsDefined(deliveryStatus))
        {
            throw new ArgumentException("Delivery status is invalid.", nameof(deliveryStatus));
        }

        if (deliveryStatus == DeliveryStatus.Delivered &&
            (!sequenceNum.HasValue || deliveredAtUtc is null))
        {
            throw new InvalidOperationException(
                "Delivered chat message must have a sequence number and delivery timestamp.");
        }

        if (deliveryStatus == DeliveryStatus.Pending && deliveredAtUtc is not null)
        {
            throw new InvalidOperationException(
                "Pending chat message cannot have a delivery timestamp.");
        }

        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Text = text.Trim();
        SentAtUtc = sentAtUtc;
        _recipientUserIds = recipientUserIds;
        SequenceNum = sequenceNum;
        DeliveryStatus = deliveryStatus;
        DeliveredAtUtc = deliveredAtUtc;
    }

    public static ChatMessageV2 Create(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        IEnumerable<Id<UserProfileMarker>> recipientUserIds,
        UtcDateTimeOffset? sentAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(id);

        var normalizedRecipientUserIds = NormalizeRecipientUserIds(recipientUserIds);
        var chatMessage = new ChatMessageV2(
            id,
            conversationId,
            senderUserId,
            text,
            sentAtUtc ?? UtcDateTimeOffset.UtcNow,
            normalizedRecipientUserIds);

        chatMessage.AddDomainEvent(new ChatMessageSentDomainEventV2(
            chatMessage.Id,
            chatMessage.ConversationId,
            chatMessage.SenderUserId,
            chatMessage.Text,
            chatMessage.SentAtUtc,
            chatMessage.RecipientUserIds));

        return chatMessage;
    }

    public static ChatMessageV2 Restore(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        UtcDateTimeOffset sentAtUtc,
        IEnumerable<Id<UserProfileMarker>> recipientUserIds,
        long? sequenceNum,
        DeliveryStatus deliveryStatus,
        UtcDateTimeOffset? deliveredAtUtc)
    {
        return new ChatMessageV2(
            id,
            conversationId,
            senderUserId,
            text,
            sentAtUtc,
            NormalizeRecipientUserIds(recipientUserIds),
            sequenceNum,
            deliveryStatus,
            deliveredAtUtc);
    }

    public bool SetSequenceNumber(long sequenceNum)
    {
        if (SequenceNum.HasValue)
        {
            return false;
        }

        if (sequenceNum <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequenceNum),
                "Sequence number must be greater than zero.");
        }

        SequenceNum = sequenceNum;
        return true;
    }

    public bool MarkAsDelivered(UtcDateTimeOffset deliveredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(deliveredAtUtc);

        if (!SequenceNum.HasValue)
        {
            throw new InvalidOperationException(
                "Sequence number must be set before marking a chat message as delivered.");
        }

        if (DeliveryStatus == DeliveryStatus.Delivered)
        {
            return false;
        }

        DeliveredAtUtc = deliveredAtUtc;
        DeliveryStatus = DeliveryStatus.Delivered;
        return true;
    }

    private static Guid[] NormalizeRecipientUserIds(
        IEnumerable<Id<UserProfileMarker>> recipientUserIds)
    {
        ArgumentNullException.ThrowIfNull(recipientUserIds);

        var normalizedRecipientUserIds = recipientUserIds
            .Where(userId => userId is not null)
            .Distinct()
            .Select(userId => userId.Value)
            .ToArray();

        if (normalizedRecipientUserIds.Length == 0)
        {
            throw new InvalidOperationException(
                "Chat message must contain at least one valid recipient.");
        }

        return normalizedRecipientUserIds;
    }
}
