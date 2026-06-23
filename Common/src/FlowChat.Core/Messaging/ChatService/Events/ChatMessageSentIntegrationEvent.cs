namespace FlowChat.Core.Messaging.ChatService.Events;

public sealed record ChatMessageSentIntegrationEvent : IntegrationEvent
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public required string SenderDisplayName { get; init; }
    public required string Text { get; init; }
    public DateTimeOffset SentAtUtc { get; init; }
    public List<Guid> RecipientUserIds { get; init; } = [];
}
