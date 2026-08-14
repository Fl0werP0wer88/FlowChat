namespace FlowChat.Core.Messaging.ChatService.Events;

public sealed record ChatMessageSentIntegrationEventV2 : IntegrationEvent
{
    public required Guid MessageId { get; init; }
    public required Guid ConversationId { get; init; }
    public required Guid SenderUserId { get; init; }
    public required string Text { get; init; }
    public required DateTimeOffset SentAtUtc { get; init; }
    public required long SequenceNum { get; init; }
    public required int ConversationMembershipRevision { get; init; }
}
