using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class ParticipantAddedDomainEvent(
    Id<Conversation> aggregateId,
    Guid participantUserId,
    UtcDateTimeOffset? occurredOnUtc = null) : BaseConversationDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid ConversationId { get; } = aggregateId.Value;
    public Guid ParticipantUserId { get; } = participantUserId;
}
