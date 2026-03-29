namespace FlowChat.Core.Messaging.AuthService.Events;

public sealed class AccountConfirmedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
}
