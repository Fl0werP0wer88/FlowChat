namespace FlowChat.Messaging.Contracts.AuthService.Events;

public sealed class EmailConfirmedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }

    public required string Email { get; init; }
}
