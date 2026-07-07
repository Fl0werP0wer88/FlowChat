using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class GroupConversationCreatedDomainEvent(
    Id<Conversation> aggregateId,
    ConversationType type,
    string? name,
    Id<UserProfileMarker> createdByUserId,
    UtcDateTimeOffset? occurredOnUtc = null) : BaseConversationDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid ConversationId { get; } = aggregateId.Value;
    public ConversationType Type { get; } = type;
    public string? Name { get; } = name;
    public Id<UserProfileMarker> CreatedByUserId { get; } = createdByUserId;
}
