using FlowChat.Core.Domain;

namespace FlowChat.Core.Messaging.PresenceService.Events;

public sealed class UserStatusChangedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public UserStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
