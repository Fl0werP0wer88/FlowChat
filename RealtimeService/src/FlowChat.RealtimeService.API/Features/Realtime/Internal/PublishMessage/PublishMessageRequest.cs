namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishMessage;

public sealed class PublishMessageRequest
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string? SenderDisplayName { get; init; }
    public string? Text { get; init; }
    public DateTime SentAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
