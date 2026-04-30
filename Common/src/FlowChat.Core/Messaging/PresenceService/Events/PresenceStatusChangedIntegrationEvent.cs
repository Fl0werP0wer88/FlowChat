using FlowChat.Core.Domain;

namespace FlowChat.Core.Messaging.PresenceService.Events;

public sealed record PresenceStatusChangedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public PresenceStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
    public List<Guid> RecipientUserIds { get; init; } = [];
}
