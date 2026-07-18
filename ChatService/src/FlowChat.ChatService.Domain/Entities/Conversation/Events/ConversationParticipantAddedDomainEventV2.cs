using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class ConversationParticipantAddedDomainEventV2(
    Id<ConversationMembership> aggregateId,
    Id<ConversationV2> conversationId,
    Id<UserProfileMarker> userId,
    long initialReadCursor,
    UtcDateTimeOffset? occurredOnUtc = null)
    : BaseConversationMembershipDomainEventV2(aggregateId, occurredOnUtc)
{
    public Id<ConversationV2> ConversationId { get; } = conversationId;
    public Id<UserProfileMarker> UserId { get; } = userId;
    public long InitialReadCursor { get; } = initialReadCursor >= 0
        ? initialReadCursor
        : throw new ArgumentOutOfRangeException(
            nameof(initialReadCursor),
            "Initial read cursor cannot be negative.");
}
