using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Consumers.Realtime.Contracts;

public sealed class PublishPresenceChangeRequest : IConsumerOutput
{
    public Guid UserId { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
