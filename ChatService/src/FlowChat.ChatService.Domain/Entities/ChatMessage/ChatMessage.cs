using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage;

public sealed class ChatMessage : AggregateRootBase<ChatMessage>
{
    public Id<ConversationAggregate> ConversationId { get; private set; }
    public Id<UserProfileMarker> SenderUserId { get; private set; }
    public string Text { get; private set; }
    public UtcDateTimeOffset SentAtUtc { get; private set; } = UtcDateTimeOffset.UtcNow;
    public UtcDateTimeOffset? DeliveredAtUtc { get; private set; }
    private Guid[] _recipientUserIds = [];
    public IReadOnlyCollection<Id<UserProfileMarker>> RecipientUserIds => [.. _recipientUserIds.Select(Id<UserProfileMarker>.FromGuid)];
    public long? SequenceNum { get; private set; }
    public DeliveryStatus DeliveryStatus { get; private set; } = DeliveryStatus.Pending;

    private ChatMessage(
        Id<ChatMessage> id,
        Id<ConversationAggregate> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        UtcDateTimeOffset sentAtUtc,
        Guid[] recipientUserIds) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(senderUserId);

        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Text = text.Trim();
        SentAtUtc = sentAtUtc;
        _recipientUserIds = recipientUserIds;
    }
    //ToDo1: Pomyśleć czy nie da rady zrobić tak aby conversationVersionAtSend dodawalo się o eventu z poziomu CommandHandlera
    public static ChatMessage Create(
        Id<ChatMessage> id,
        Id<ConversationAggregate> conversationId,
        Id<UserProfileMarker> senderUserId,
        string text,
        IEnumerable<Id<UserProfileMarker>> recipientUserIds,
        int conversationVersionAtSend,
        UtcDateTimeOffset? sentAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        var normalizedRecipientUserIds = NormalizeRecipientUserIds(recipientUserIds);
        var chatMessage = new ChatMessage(
            id,
            conversationId,
            senderUserId,
            text,
            sentAtUtc ?? UtcDateTimeOffset.UtcNow,
            normalizedRecipientUserIds);

        chatMessage.AddDomainEvent(
            new ChatMessageSentDomainEvent(
                chatMessage.Id,
                chatMessage.ConversationId,
                chatMessage.SenderUserId,
                chatMessage.Text,
                chatMessage.SentAtUtc,
                conversationVersionAtSend));

        return chatMessage;
    }

    public void SetSequenceNumber(long sequenceNum)
    {
        if (SequenceNum.HasValue)
            return;

        if (sequenceNum <= 0)
            throw new ArgumentException("Sequence number must be greater than zero.", nameof(sequenceNum));

        SequenceNum = sequenceNum;
        AddDomainEvent(new ChatMessageSequencedDomainEvent(Id, ConversationId, sequenceNum));
    }

    public void MarkAsDelivered(UtcDateTimeOffset deliveredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(deliveredAtUtc);

        if (!SequenceNum.HasValue)
            throw new InvalidOperationException("Sequence number must be set before marking a chat message as delivered.");

        if (DeliveryStatus == DeliveryStatus.Delivered)
            return;

        DeliveredAtUtc = deliveredAtUtc;
        DeliveryStatus = DeliveryStatus.Delivered;
    }

    private static Guid[] NormalizeRecipientUserIds(IEnumerable<Id<UserProfileMarker>> recipientUserIds)
    {
        ArgumentNullException.ThrowIfNull(recipientUserIds);

        var normalizedRecipientUserIds = recipientUserIds
            .Where(userId => userId is not null)
            .Distinct()
            .Select(userId => userId.Value)
            .ToArray();

        if (normalizedRecipientUserIds.Length == 0)
        {
            throw new InvalidOperationException("Chat message must contain at least one valid recipient.");
        }

        return normalizedRecipientUserIds;
    }
}

