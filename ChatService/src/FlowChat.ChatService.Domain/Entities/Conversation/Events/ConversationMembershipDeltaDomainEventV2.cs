using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class ConversationMembershipDeltaDomainEventV2(
    Id<ConversationMembership> aggregateId,
    Id<ConversationV2> conversationId,
    ConversationMembershipDeltaOperation operation,
    IReadOnlyCollection<Id<UserProfileMarker>> userIds,
    int participantCount,
    UtcDateTimeOffset? occurredOnUtc = null)
    : BaseConversationMembershipDomainEventV2(aggregateId, occurredOnUtc)
{
    public Id<ConversationV2> ConversationId { get; } = conversationId;
    public ConversationMembershipDeltaOperation Operation { get; } = operation;
    public IReadOnlyCollection<Id<UserProfileMarker>> UserIds { get; } =
        userIds?.ToArray() ?? throw new ArgumentNullException(nameof(userIds));
    public int ParticipantCount { get; } = participantCount;
}
