using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class ConversationCreatedDomainEvent(
    Id<Conversation> aggregateId,
    ConversationType type,
    string? name,
    Guid createdByUserId,
    IReadOnlyCollection<Guid> participantUserIds,
    UtcDateTimeOffset? occurredOnUtc = null) : BaseConversationDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid ConversationId { get; } = aggregateId.Value;
    public ConversationType Type { get; } = type;
    public string? Name { get; } = name;
    public Guid CreatedByUserId { get; } = createdByUserId;
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; } = participantUserIds;
}
