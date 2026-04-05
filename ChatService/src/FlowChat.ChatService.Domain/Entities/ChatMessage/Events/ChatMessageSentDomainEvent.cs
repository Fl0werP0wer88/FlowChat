using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage.Events;

public sealed class ChatMessageSentDomainEvent(
    Id<ChatMessage> aggregateId,
    Guid conversationId,
    Guid senderUserId,
    string senderDisplayName,
    string text,
    DateTimeOffset sentAtUtc,
    IReadOnlyCollection<Guid> recipientUserIds,
    DateTimeOffset? occurredOnUtc = null) : BaseChatMessageDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid MessageId { get; } = aggregateId.Value;
    public Guid ConversationId { get; } = conversationId;
    public Guid SenderUserId { get; } = senderUserId;
    public string SenderDisplayName { get; } = senderDisplayName;
    public string Text { get; } = text;
    public DateTimeOffset SentAtUtc { get; } = sentAtUtc;
    public IReadOnlyCollection<Guid> RecipientUserIds { get; } = recipientUserIds;
}

