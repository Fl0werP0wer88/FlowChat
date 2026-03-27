namespace FlowChat.Core.Messaging.AuthService.Events;

public sealed class UserConfirmedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
}
