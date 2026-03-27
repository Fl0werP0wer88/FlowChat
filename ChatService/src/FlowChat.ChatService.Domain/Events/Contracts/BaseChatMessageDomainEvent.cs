using FlowChat.ChatService.Domain.Common.Constants;
using FlowChat.ChatService.Domain.Entities;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Domain.Events.Contracts;

[AggregateType(ChatMessageConstants.ChatMessageAggregateTypeName)]
public abstract class BaseChatMessageDomainEvent(
    Id<ChatMessage> aggregateId,
    DateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? DateTimeOffset.UtcNow)
{
}

