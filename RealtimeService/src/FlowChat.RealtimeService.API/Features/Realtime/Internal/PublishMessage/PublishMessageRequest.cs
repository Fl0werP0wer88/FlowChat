using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishMessage;

public sealed class PublishMessageRequest : IServiceInput
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string? Text { get; init; }
    public long SequenceNum { get; init; }
    public DateTimeOffset SentAtUtc { get; init; }
    public DateTimeOffset DeliveredAtUtc { get; init; }
}
