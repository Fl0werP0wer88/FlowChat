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
    public long SequenceNum { get; private set; }
    public DeliveryStatus DeliveryStatus { get; private set; }

    private ChatMessageV2(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        UtcDateTimeOffset sentAtUtc,
        long sequenceNum,
        DeliveryStatus deliveryStatus = DeliveryStatus.Pending,
        UtcDateTimeOffset? deliveredAtUtc = null) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(senderUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (sequenceNum <= 0)
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
            deliveredAtUtc is null)
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
        SequenceNum = sequenceNum;
        DeliveryStatus = deliveryStatus;
        DeliveredAtUtc = deliveredAtUtc;
    }

    public static ChatMessageV2 Create(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        long sequenceNum,
        UtcDateTimeOffset? sentAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(id);

        var chatMessage = new ChatMessageV2(
            id,
            conversationId,
            senderUserId,
            text,
            sentAtUtc ?? UtcDateTimeOffset.UtcNow,
            sequenceNum);

        chatMessage.AddDomainEvent(new ChatMessageSentDomainEventV2(
            chatMessage.Id,
            chatMessage.ConversationId,
            chatMessage.SenderUserId,
            chatMessage.Text,
            chatMessage.SentAtUtc,
            chatMessage.SequenceNum));

        return chatMessage;
    }

    public static ChatMessageV2 Restore(
        Id<ChatMessageV2> id,
        Id<ConversationV2> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        UtcDateTimeOffset sentAtUtc,
        long sequenceNum,
        DeliveryStatus deliveryStatus,
        UtcDateTimeOffset? deliveredAtUtc)
    {
        return new ChatMessageV2(
            id,
            conversationId,
            senderUserId,
            text,
            sentAtUtc,
            sequenceNum,
            deliveryStatus,
            deliveredAtUtc);
    }

    public bool MarkAsDelivered(UtcDateTimeOffset deliveredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(deliveredAtUtc);

        if (DeliveryStatus == DeliveryStatus.Delivered)
        {
            return false;
        }

        DeliveredAtUtc = deliveredAtUtc;
        DeliveryStatus = DeliveryStatus.Delivered;
        return true;
    }

}
