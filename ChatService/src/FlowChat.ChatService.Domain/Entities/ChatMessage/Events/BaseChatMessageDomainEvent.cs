using FlowChat.ChatService.Domain.Common.Constants;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage.Events;

[AggregateType(ChatMessageConstants.ChatMessageAggregateTypeName)]
public abstract class BaseChatMessageDomainEvent(
    Id<ChatMessage> aggregateId,
    UtcDateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? UtcDateTimeOffset.UtcNow)
{
}

