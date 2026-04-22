using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage;

public sealed class ChatMessage : AggregateRootBase<ChatMessage>
{
    public Id<ConversationAggregate> ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string SenderDisplayName { get; private set; }
    public string Text { get; private set; }
    public UtcDateTimeOffset SentAtUtc { get; private set; } = UtcDateTimeOffset.UtcNow;
    public Guid[] RecipientUserIds { get; private set; }

    private ChatMessage(
        Id<ChatMessage> id,
        Id<ConversationAggregate> conversationId,
        Guid senderUserId,
        string senderDisplayName,
        string text,
        UtcDateTimeOffset sentAtUtc,
        Guid[] recipientUserIds) : base(id)
    {
        ArgumentNullException.ThrowIfNull(conversationId);

        if (senderUserId == Guid.Empty)
        {
            throw new ArgumentException("SenderUserId is required.", nameof(senderUserId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(senderDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        ConversationId = conversationId;
        SenderUserId = senderUserId;
        SenderDisplayName = senderDisplayName.Trim();
        Text = text.Trim();
        SentAtUtc = sentAtUtc;
        RecipientUserIds = NormalizeRecipientUserIds(recipientUserIds);
    }

    public static ChatMessage Create(
        Id<ConversationAggregate> conversationId,
        Guid senderUserId,
        string senderDisplayName,
        string text,
        IEnumerable<Guid> recipientUserIds,
        UtcDateTimeOffset? sentAtUtc = null,
        Id<ChatMessage>? id = null)
    {
        var normalizedRecipientUserIds = NormalizeRecipientUserIds(recipientUserIds);
        var chatMessage = new ChatMessage(
            id ?? Id<ChatMessage>.New(),
            conversationId,
            senderUserId,
            senderDisplayName,
            text,
            sentAtUtc ?? UtcDateTimeOffset.UtcNow,
            normalizedRecipientUserIds);

        chatMessage.AddDomainEvent(
            new ChatMessageSentDomainEvent(
                chatMessage.Id,
                chatMessage.ConversationId.Value,
                chatMessage.SenderUserId,
                chatMessage.SenderDisplayName,
                chatMessage.Text,
                chatMessage.SentAtUtc,
                chatMessage.RecipientUserIds));

        return chatMessage;
    }

    private static Guid[] NormalizeRecipientUserIds(IEnumerable<Guid> recipientUserIds)
    {
        ArgumentNullException.ThrowIfNull(recipientUserIds);

        // Guid.Empty is filtered out because callers may pass uninitialized or placeholder IDs.
        // Deduplication prevents the same user receiving the same message notification multiple times.
        var normalizedRecipientUserIds = recipientUserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedRecipientUserIds.Length == 0)
        {
            throw new InvalidOperationException("Chat message must contain at least one valid recipient.");
        }

        return normalizedRecipientUserIds;
    }
}

