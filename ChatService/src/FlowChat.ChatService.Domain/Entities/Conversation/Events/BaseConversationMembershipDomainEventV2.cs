using FlowChat.ChatService.Domain.Entities.Conversation.Constants;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

[AggregateType(ConversationMembershipConstants.ConversationMembershipAggregateTypeName)]
public abstract class BaseConversationMembershipDomainEventV2(
    Id<ConversationMembership> aggregateId,
    UtcDateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? UtcDateTimeOffset.UtcNow)
{
}
