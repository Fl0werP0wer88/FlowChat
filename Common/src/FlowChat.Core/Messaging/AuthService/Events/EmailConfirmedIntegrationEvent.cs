namespace FlowChat.Core.Messaging.AuthService.Events;

public sealed class EmailConfirmedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }

    public required Email Email { get; init; }
}
