using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class ConversationParticipantsAddedDomainEventV2(
    Id<ConversationMembership> aggregateId,
    Id<ConversationV2> conversationId,
    IReadOnlyList<Id<UserProfileMarker>> participantUserIds,
    long initialReadCursor,
    UtcDateTimeOffset? occurredOnUtc = null)
    : BaseConversationMembershipDomainEventV2(aggregateId, occurredOnUtc),
      IConversationParticipantsChangedDomainEventV2
{
    public Id<ConversationV2> ConversationId { get; } = conversationId;
    public IReadOnlyList<Id<UserProfileMarker>> ParticipantUserIds { get; } =
        participantUserIds?.ToArray()
        ?? throw new ArgumentNullException(nameof(participantUserIds));
    public long InitialReadCursor { get; } = initialReadCursor >= 0
        ? initialReadCursor
        : throw new ArgumentOutOfRangeException(
            nameof(initialReadCursor),
            "Initial read cursor cannot be negative.");
}
