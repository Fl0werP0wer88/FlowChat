namespace FlowChat.Core.Messaging.ChatService.Events;

public sealed class ChatMessageSentIntegrationEvent : IntegrationEvent
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public required string SenderDisplayName { get; init; }
    public required string Text { get; init; }
    public DateTime SentAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
