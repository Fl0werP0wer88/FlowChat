namespace FlowChat.Core.Messaging.AuthService.Events;

public sealed record AccountConfirmedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
}
