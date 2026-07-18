using FlowChat.ChatService.Domain.Entities.ChatMessage.Constants;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage.Events;

[AggregateType(ChatMessageV2Constants.ChatMessageAggregateTypeName)]
public abstract class BaseChatMessageDomainEventV2(
    Id<ChatMessageV2> aggregateId,
    UtcDateTimeOffset? occurredOnUtc = null)
    : DomainEventBase(aggregateId, occurredOnUtc ?? UtcDateTimeOffset.UtcNow)
{
}
