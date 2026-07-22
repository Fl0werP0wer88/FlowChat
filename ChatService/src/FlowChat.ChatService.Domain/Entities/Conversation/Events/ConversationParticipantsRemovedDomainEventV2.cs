using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class ConversationParticipantsRemovedDomainEventV2(
    Id<ConversationMembership> aggregateId,
    Id<ConversationV2> conversationId,
    IReadOnlyList<Id<UserProfileMarker>> participantUserIds,
    UtcDateTimeOffset? occurredOnUtc = null)
    : BaseConversationMembershipDomainEventV2(aggregateId, occurredOnUtc)
{
    public Id<ConversationV2> ConversationId { get; } = conversationId;
    public IReadOnlyList<Id<UserProfileMarker>> ParticipantUserIds { get; } =
        participantUserIds?.ToArray()
        ?? throw new ArgumentNullException(nameof(participantUserIds));
}
