using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.ConversationV2;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage.Events;

public sealed class ConversationParticipantUnhideRequestedDomainEventV2(
    Id<ChatMessageV2> aggregateId,
    Id<ConversationParticipant> conversationParticipantId,
    Id<ConversationAggregate> conversationId,
    Id<UserProfileMarker> userId,
    UtcDateTimeOffset? occurredOnUtc = null)
    : BaseChatMessageDomainEventV2(aggregateId, occurredOnUtc)
{
    public Guid MessageId { get; } = aggregateId.Value;
    public Id<ConversationParticipant> ConversationParticipantId { get; } = conversationParticipantId;
    public Id<ConversationAggregate> ConversationId { get; } = conversationId;
    public Id<UserProfileMarker> UserId { get; } = userId;
}
