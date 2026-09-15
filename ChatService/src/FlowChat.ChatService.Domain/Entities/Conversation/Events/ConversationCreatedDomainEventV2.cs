using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.Conversation.Events;

public sealed class ConversationCreatedDomainEventV2(
    Id<ConversationV2> aggregateId,
    ConversationType type,
    string? name,
    IReadOnlyCollection<Id<UserProfileMarker>> participantUserIds,
    UtcDateTimeOffset? occurredOnUtc = null)
    : BaseConversationDomainEventV2(aggregateId, occurredOnUtc)
{
    public Guid ConversationId { get; } = aggregateId.Value;
    public ConversationType Type { get; } = type;
    public string? Name { get; } = name;
    public IReadOnlyCollection<Id<UserProfileMarker>> ParticipantUserIds { get; } =
        participantUserIds?.ToArray() ?? throw new ArgumentNullException(nameof(participantUserIds));
}
