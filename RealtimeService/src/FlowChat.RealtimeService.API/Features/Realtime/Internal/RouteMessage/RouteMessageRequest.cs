using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.RouteMessage;

public sealed class RouteMessageRequest : IServiceInput
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string? SenderDisplayName { get; init; }
    public string? Text { get; init; }
    public DateTimeOffset SentAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
