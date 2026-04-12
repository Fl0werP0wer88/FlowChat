using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Consumers.Realtime.Contracts;

public sealed class PublishPresenceChangeRequest : IConsumerOutput
{
    public Guid UserId { get; init; }
    public UserPresenceStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
