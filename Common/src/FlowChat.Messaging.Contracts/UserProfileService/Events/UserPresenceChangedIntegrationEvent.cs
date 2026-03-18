namespace FlowChat.Messaging.Contracts.UserProfileService.Events;

public sealed class UserPresenceChangedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public required string Status { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
