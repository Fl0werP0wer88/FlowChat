using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class ParticipantAddedDomainEvent(
    Id<Conversation> aggregateId,
    Id<UserProfileMarker> participantUserId,
    UtcDateTimeOffset? occurredOnUtc = null) : BaseConversationDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid ConversationId { get; } = aggregateId.Value;
    public Id<UserProfileMarker> ParticipantUserId { get; } = participantUserId;
}
