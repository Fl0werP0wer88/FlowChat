using FlowChat.ChatService.Domain.Entities.Conversation.Constants;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

[AggregateType(ConversationV2Constants.ConversationAggregateTypeName)]
public abstract class BaseConversationDomainEventV2(
    Id<ConversationV2> aggregateId,
    UtcDateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? UtcDateTimeOffset.UtcNow)
{
}
