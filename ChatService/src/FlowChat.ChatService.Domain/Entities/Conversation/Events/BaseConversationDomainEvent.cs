using FlowChat.ChatService.Domain.Entities.Conversation.Constants;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

[AggregateType(ConversationConstants.ConversationAggregateTypeName)]
public abstract class BaseConversationDomainEvent(
    Id<Conversation> aggregateId,
    UtcDateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? UtcDateTimeOffset.UtcNow)
{
}
