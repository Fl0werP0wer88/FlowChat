using FlowChat.ChatService.Domain.Events;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage;

public sealed class ChatMessage : AggregateRootBase<ChatMessage>
{
    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string SenderDisplayName { get; private set; }
    public string Text { get; private set; }
    public DateTime SentAtUtc { get; private set; }
    public Guid[] RecipientUserIds { get; private set; }

    private ChatMessage(
        Id<ChatMessage>? id,
        Guid conversationId,
        Guid senderUserId,
        string senderDisplayName,
        string text,
        DateTime sentAtUtc,
        Guid[] recipientUserIds) : base(id)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("ConversationId is required.", nameof(conversationId));
        }

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
        SentAtUtc = DateTime.SpecifyKind(sentAtUtc, DateTimeKind.Utc);
        RecipientUserIds = NormalizeRecipientUserIds(recipientUserIds);
    }

    public static ChatMessage Create(
        Guid conversationId,
        Guid senderUserId,
        string senderDisplayName,
        string text,
        IEnumerable<Guid> recipientUserIds,
        DateTime? sentAtUtc = null,
        Id<ChatMessage>? id = null)
    {
        var normalizedRecipientUserIds = NormalizeRecipientUserIds(recipientUserIds);
        var chatMessage = new ChatMessage(
            id ?? Id<ChatMessage>.New(),
            conversationId,
            senderUserId,
            senderDisplayName,
            text,
            sentAtUtc ?? DateTime.UtcNow,
            normalizedRecipientUserIds);

        chatMessage.AddDomainEvent(
            new ChatMessageSentDomainEvent(
                chatMessage.Id,
                chatMessage.ConversationId,
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

