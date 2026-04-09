using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Consumers.Realtime.Contracts;

public sealed class PublishMessageRequest : IConsumerOutput
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string? SenderDisplayName { get; init; }
    public string? Text { get; init; }
    public DateTimeOffset SentAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
