using FlowChat.ChatService.Domain.Entities;
using FlowChat.ChatService.Domain.Events.Contracts;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Domain.Events;

public sealed class ChatMessageSentDomainEvent(
    Id<ChatMessage> aggregateId,
    Guid conversationId,
    Guid senderUserId,
    string senderDisplayName,
    string text,
    DateTime sentAtUtc,
    IReadOnlyCollection<Guid> recipientUserIds,
    DateTimeOffset? occurredOnUtc = null) : BaseChatMessageDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid MessageId { get; } = aggregateId.Value;
    public Guid ConversationId { get; } = conversationId;
    public Guid SenderUserId { get; } = senderUserId;
    public string SenderDisplayName { get; } = senderDisplayName;
    public string Text { get; } = text;
    public DateTime SentAtUtc { get; } = sentAtUtc;
    public IReadOnlyCollection<Guid> RecipientUserIds { get; } = recipientUserIds;
}

