using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.ConversationV2;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage.Events;

public sealed class ChatMessageSentDomainEventV2(
    Id<ChatMessageV2> aggregateId,
    Id<ConversationAggregate> conversationId,
    Id<UserProfileMarker> senderUserId,
    string text,
    UtcDateTimeOffset sentAtUtc,
    IReadOnlyCollection<Id<UserProfileMarker>> recipientUserIds,
    UtcDateTimeOffset? occurredOnUtc = null)
    : BaseChatMessageDomainEventV2(aggregateId, occurredOnUtc)
{
    public Guid MessageId { get; } = aggregateId.Value;
    public Id<ConversationAggregate> ConversationId { get; } = conversationId;
    public Id<UserProfileMarker> SenderUserId { get; } = senderUserId;
    public string Text { get; } = text;
    public UtcDateTimeOffset SentAtUtc { get; } = sentAtUtc;
    public IReadOnlyCollection<Id<UserProfileMarker>> RecipientUserIds { get; } =
        recipientUserIds?.ToArray() ?? throw new ArgumentNullException(nameof(recipientUserIds));
}
