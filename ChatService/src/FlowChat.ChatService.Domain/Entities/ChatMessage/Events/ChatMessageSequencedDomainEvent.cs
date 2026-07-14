using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage.Events;

public sealed class ChatMessageSequencedDomainEvent(
    Id<ChatMessage> aggregateId,
    Id<ConversationAggregate> conversationId,
    long sequenceNum,
    UtcDateTimeOffset? occurredOnUtc = null) : BaseChatMessageDomainEvent(aggregateId, occurredOnUtc)
{
    public Id<ChatMessage> MessageId { get; } = aggregateId;
    public Id<ConversationAggregate> ConversationId { get; } = conversationId;
    public long SequenceNum { get; } = sequenceNum;
}
