using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage.Events;

public sealed class ChatMessageSentDomainEvent(
    Id<ChatMessage> aggregateId,
    Id<ConversationAggregate> conversationId,
    Id<UserProfileMarker> senderUserId,
    string text,
    UtcDateTimeOffset sentAtUtc,
    IReadOnlyCollection<Id<UserProfileMarker>> recipientUserIds,
    UtcDateTimeOffset? occurredOnUtc = null) : BaseChatMessageDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid MessageId { get; } = aggregateId.Value;
    public Id<ConversationAggregate> ConversationId { get; } = conversationId;
    public Id<UserProfileMarker> SenderUserId { get; } = senderUserId;
    public string Text { get; } = text;
    public UtcDateTimeOffset SentAtUtc { get; } = sentAtUtc;
    public IReadOnlyCollection<Id<UserProfileMarker>> RecipientUserIds { get; } = recipientUserIds;
}

