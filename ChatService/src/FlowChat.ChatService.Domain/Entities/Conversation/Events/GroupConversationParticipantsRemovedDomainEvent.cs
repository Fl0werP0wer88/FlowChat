using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class GroupConversationParticipantsRemovedDomainEvent(
    Id<Conversation> aggregateId,
    IReadOnlyCollection<Id<UserProfileMarker>> participantUserIds,
    int conversationMembershipRevision,
    UtcDateTimeOffset? occurredOnUtc = null) : BaseConversationDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid ConversationId { get; } = aggregateId.Value;
    public IReadOnlyCollection<Id<UserProfileMarker>> ParticipantUserIds { get; } = participantUserIds;
    public int ConversationMembershipRevision { get; } = conversationMembershipRevision;
}
